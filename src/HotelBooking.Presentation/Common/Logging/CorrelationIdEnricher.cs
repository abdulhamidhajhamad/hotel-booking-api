using Microsoft.AspNetCore.Http;
using Serilog.Core;
using Serilog.Events;

namespace HotelBooking.Presentation.Common.Logging;

public sealed class CorrelationIdEnricher : ILogEventEnricher
{
    public const string HttpContextItemsKey = "CorrelationId";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var correlationId = _httpContextAccessor.HttpContext?.Items[HttpContextItemsKey] as string;
        if (!string.IsNullOrEmpty(correlationId))
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("CorrelationId", correlationId));
        }
    }
}