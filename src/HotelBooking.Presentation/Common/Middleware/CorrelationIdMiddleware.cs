using Microsoft.AspNetCore.Http;
using HotelBooking.Presentation.Common.Logging;

namespace HotelBooking.Presentation.Common.Middleware;

public sealed class CorrelationIdMiddleware
{
    private const string HeaderName = "X-Correlation-Id";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ReadOrGenerate(context);

        context.Items[CorrelationIdEnricher.HttpContextItemsKey] = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        await _next(context);
    }

    private static string ReadOrGenerate(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var value))
        {
            var incoming = value.ToString();
            if (!string.IsNullOrWhiteSpace(incoming) && incoming.Length <= 64)
                return incoming;
        }

        return Guid.NewGuid().ToString("n")[..16];
    }
}