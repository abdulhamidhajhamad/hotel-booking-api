using HotelBooking.Application.Features.Bookings.Common;
using HotelBooking.Domain.Common;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Bookings;

public sealed class ExpiredHoldSweeper : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ExpiredHoldSweeperOptions _options;
    private readonly ILogger<ExpiredHoldSweeper> _logger;

    public ExpiredHoldSweeper(
        IServiceScopeFactory scopeFactory,
        IOptions<ExpiredHoldSweeperOptions> options,
        ILogger<ExpiredHoldSweeper> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expired-hold sweeper started (interval={Interval}s, ttl={Ttl}m).",
            _options.SweepIntervalSeconds, BookingHold.TtlMinutes);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.SweepIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Expired-hold sweeper failed; will retry on the next tick.");
            }
        }

        _logger.LogInformation("Expired-hold sweeper stopped.");
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var threshold = DateTimeOffset.UtcNow.AddMinutes(-BookingHold.TtlMinutes);

        var expiredIds = await db.Bookings
            .Where(b => b.Status == BookingStatus.Pending && b.CreatedAt < threshold)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        if (expiredIds.Count == 0)
            return;

        await db.RoomAvailability
            .Where(a => a.BookingId != null && expiredIds.Contains(a.BookingId.Value))
            .ExecuteDeleteAsync(cancellationToken);

        await db.Bookings
            .Where(b => expiredIds.Contains(b.Id) && b.Status == BookingStatus.Pending)
            .ExecuteUpdateAsync(
                s => s.SetProperty(b => b.Status, BookingStatus.Cancelled),
                cancellationToken);

        _logger.LogInformation("Expired-hold sweeper released {Count} pending booking hold(s).", expiredIds.Count);
    }
}