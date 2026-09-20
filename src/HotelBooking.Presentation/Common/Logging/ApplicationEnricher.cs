using Serilog.Core;
using Serilog.Events;

namespace HotelBooking.Presentation.Common.Logging;

public sealed class ApplicationEnricher : ILogEventEnricher
{
    private const string ApplicationName = "HotelBooking.Api";

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("Application", ApplicationName));
    }
}