using HotelBooking.Domain.Common;

namespace HotelBooking.Application.Abstractions.Outbox;

public interface IOutboxHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
}
