using System.Text.Json;
using HotelBooking.Domain.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HotelBooking.Infrastructure.Outbox;

public sealed class DomainEventsToOutboxInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var entities = context.ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        if (entities.Count == 0)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var outbox = context.Set<OutboxMessage>();

        foreach (var entity in entities)
        {
            foreach (var domainEvent in entity.DomainEvents)
            {
                outbox.Add(new OutboxMessage
                {
                    Id = domainEvent.EventId == Guid.Empty ? Guid.NewGuid() : domainEvent.EventId,
                    Type = domainEvent.GetType().FullName!,
                    Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), SerializerOptions),
                    OccurredAtUtc = domainEvent.OccurredAtUtc == default ? now : domainEvent.OccurredAtUtc,
                    NextAttemptAtUtc = now,
                    Status = OutboxMessageStatus.Pending,
                    AttemptCount = 0,
                });
            }

            entity.ClearDomainEvents();
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
