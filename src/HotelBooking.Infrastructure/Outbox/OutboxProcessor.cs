using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Outbox;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OutboxSignal _signal;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        OutboxSignal signal,
        IOptions<OutboxOptions> options,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _signal = signal;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started (batch={Batch}, fallback={Fallback}s, maxAttempts={Max}).",
            _options.BatchSize, _options.PollFallbackSeconds, _options.MaxAttempts);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await DrainBatchAsync(stoppingToken);

                if (processed < _options.BatchSize)
                {
                    await _signal.WaitAsync(
                        TimeSpan.FromSeconds(_options.PollFallbackSeconds),
                        stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processor encountered an unexpected error; retrying after fallback delay.");
                await _signal.WaitAsync(
                    TimeSpan.FromSeconds(_options.PollFallbackSeconds),
                    stoppingToken);
            }
        }

        _logger.LogInformation("Outbox processor stopped.");
    }

    private async Task<int> DrainBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var dispatcher = scope.ServiceProvider.GetRequiredService<OutboxDispatcher>();

        var now = DateTimeOffset.UtcNow;

        var rows = await db.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.Pending
                     && m.NextAttemptAtUtc <= now)
            .OrderBy(m => m.OccurredAtUtc)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return 0;

        foreach (var row in rows)
        {
            try
            {
                await dispatcher.DispatchAsync(row.Type, row.Payload, cancellationToken);
                row.Status = OutboxMessageStatus.Processed;
                row.ProcessedAtUtc = DateTimeOffset.UtcNow;
                row.LastError = null;
            }
            catch (Exception ex)
            {
                row.AttemptCount++;
                row.LastError = ex.Message;

                if (row.AttemptCount >= _options.MaxAttempts)
                {
                    row.Status = OutboxMessageStatus.DeadLetter;
                    _logger.LogError(ex,
                        "Outbox row {RowId} of type {Type} moved to dead-letter after {Attempts} attempts.",
                        row.Id, row.Type, row.AttemptCount);
                }
                else
                {
                    row.NextAttemptAtUtc = DateTimeOffset.UtcNow.Add(BackoffFor(row.AttemptCount));
                    _logger.LogWarning(ex,
                        "Outbox row {RowId} of type {Type} failed on attempt {Attempts}; retry scheduled at {NextAttempt}.",
                        row.Id, row.Type, row.AttemptCount, row.NextAttemptAtUtc);
                }
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return rows.Count;
    }

    private TimeSpan BackoffFor(int attempt)
    {
        var seconds = Math.Min(
            _options.BackoffBaseSeconds * Math.Pow(2, attempt - 1),
            _options.BackoffMaxSeconds);
        return TimeSpan.FromSeconds(seconds);
    }
}
