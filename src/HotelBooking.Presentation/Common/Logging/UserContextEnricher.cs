using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;
using HotelBooking.Application.Abstractions;

namespace HotelBooking.Presentation.Common.Logging;

public sealed class UserContextEnricher : ILogEventEnricher
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserContextEnricher(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
            return;

        var currentUser = httpContext.RequestServices.GetService<ICurrentUser>();
        if (currentUser?.Id is Guid id)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty("UserId", id));
        }
    }
}