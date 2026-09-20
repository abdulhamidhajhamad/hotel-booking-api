using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using HotelBooking.Application.Behaviors;
using HotelBooking.Presentation.Common.Logging;

namespace HotelBooking.Presentation.Common.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private const string GenericProdMessage = "An unexpected error occurred.";

    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items[CorrelationIdEnricher.HttpContextItemsKey] as string ?? string.Empty;
        var requestPath = httpContext.Request.Path.Value ?? string.Empty;
        var requestMethod = httpContext.Request.Method;
        var requestName = exception.Data[LoggingBehavior<object, object>.RequestNameExceptionKey] as string ?? "(none)";

        _logger.LogError(
            exception,
            "Unhandled exception on {RequestMethod} {RequestPath} (correlation {CorrelationId}, request {RequestName})",
            requestMethod, requestPath, correlationId, requestName);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Server error",
            Type = "Server.Unhandled",
            Detail = _environment.IsDevelopment()
                ? $"{exception.GetType().Name}: {exception.Message}"
                : GenericProdMessage,
        };

        problem.Extensions["correlationId"] = correlationId;

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}