using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace HotelBooking.Presentation.Common.Logging;

public sealed class RequestContextEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RequestContextEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return;

        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestPath", httpContext.Request.Path.Value ?? string.Empty));
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("RequestMethod", httpContext.Request.Method));
    }
}