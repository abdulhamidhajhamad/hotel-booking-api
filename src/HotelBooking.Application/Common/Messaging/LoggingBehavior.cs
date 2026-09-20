using System.Diagnostics;
using HotelBooking.Application.Common.Results;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.Common.Messaging;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TResponse : Result
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger) => _logger = logger;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var name = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();

        _logger.LogInformation("Handling {Operation}", name);

        try
        {
            var response = await next();
            stopwatch.Stop();

            if (response.IsSuccess)
            {
                _logger.LogInformation(
                    "Handled {Operation} in {ElapsedMs}ms with success",
                    name, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogWarning(
                    "Handled {Operation} in {ElapsedMs}ms with failure {ErrorCode}: {ErrorMessage}",
                    name, stopwatch.ElapsedMilliseconds,
                    response.Error.Code, response.Error.Message);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Unhandled exception in {Operation} after {ElapsedMs}ms",
                name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}