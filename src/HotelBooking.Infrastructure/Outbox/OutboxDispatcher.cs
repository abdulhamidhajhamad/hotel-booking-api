using System.Text.Json;
using HotelBooking.Application.Abstractions.Outbox;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Infrastructure.Outbox;

public sealed class OutboxDispatcher
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly OutboxEventTypeRegistry _registry;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxDispatcher> _logger;

    public OutboxDispatcher(
        OutboxEventTypeRegistry registry,
        IServiceProvider serviceProvider,
        ILogger<OutboxDispatcher> logger)
    {
        _registry = registry;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(string typeName, string payload, CancellationToken cancellationToken)
    {
        if (!_registry.TryResolve(typeName, out var eventType))
            throw new InvalidOperationException($"No CLR type registered for outbox event '{typeName}'.");

        var domainEvent = JsonSerializer.Deserialize(payload, eventType, SerializerOptions)
                          ?? throw new InvalidOperationException($"Failed to deserialize payload for event '{typeName}'.");

        var handlerType = typeof(IOutboxHandler<>).MakeGenericType(eventType);
        var handlers = _serviceProvider.GetServices(handlerType).ToList();

        if (handlers.Count == 0)
        {
            _logger.LogWarning("No handler registered for outbox event {EventType}", typeName);
            return;
        }

        var method = handlerType.GetMethod("HandleAsync")!;

        foreach (var handler in handlers)
        {
            var task = (Task)method.Invoke(handler, new[] { domainEvent, cancellationToken })!;
            await task;
        }
    }
}