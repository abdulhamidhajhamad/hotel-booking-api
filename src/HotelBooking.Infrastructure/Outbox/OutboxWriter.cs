using System.Text.Json;
using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Infrastructure.Persistence;

namespace HotelBooking.Infrastructure.Outbox;

public sealed class OutboxWriter : IOutbox
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ApplicationDbContext _db;

    public OutboxWriter(ApplicationDbContext db) => _db = db;

    public Task EnqueueAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        var now = DateTimeOffset.UtcNow;
        _db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(TEvent).FullName!,
            Payload = JsonSerializer.Serialize(integrationEvent, typeof(TEvent), SerializerOptions),
            OccurredAtUtc = now,
            NextAttemptAtUtc = now,
            Status = OutboxMessageStatus.Pending,
            AttemptCount = 0,
        });
        return Task.CompletedTask;
    }
}
