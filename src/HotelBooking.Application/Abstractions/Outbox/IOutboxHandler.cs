namespace HotelBooking.Application.Abstractions.Outbox;

public interface IOutboxHandler<in TEvent> where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken cancellationToken);
}
