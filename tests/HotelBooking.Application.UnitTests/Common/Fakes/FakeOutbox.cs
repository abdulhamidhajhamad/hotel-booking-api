using HotelBooking.Application.Abstractions.Outbox;

namespace HotelBooking.Application.UnitTests.Common.Fakes;

public sealed class FakeOutbox : IOutbox
{
    public List<IIntegrationEvent> Events { get; } = new();

    public Task EnqueueAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent
    {
        Events.Add(integrationEvent);
        return Task.CompletedTask;
    }
}