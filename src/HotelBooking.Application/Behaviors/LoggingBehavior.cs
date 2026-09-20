using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace HotelBooking.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public const string RequestNameExceptionKey = "MediatR.RequestName";

    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        using var _ = LogContext.PushProperty("RequestName", requestName);

        _logger.LogDebug("Handling {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next();
            stopwatch.Stop();
            _logger.LogDebug("Handled {RequestName} in {ElapsedMs}ms", requestName, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            if (!ex.Data.Contains(RequestNameExceptionKey))
                ex.Data[RequestNameExceptionKey] = requestName;
            throw;
        }
    }
}