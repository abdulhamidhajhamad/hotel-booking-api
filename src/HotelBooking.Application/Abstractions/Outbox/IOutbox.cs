namespace HotelBooking.Application.Abstractions.Outbox;

public interface IOutbox
{
    Task EnqueueAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken)
        where TEvent : IIntegrationEvent;
}
