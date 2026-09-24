using HotelBooking.Application.Abstractions.Outbox;

namespace HotelBooking.Api.IntegrationTests.Features.Outbox;

public sealed record DeadLetterTestEvent(string Marker) : IIntegrationEvent;

public sealed class ThrowingDeadLetterHandler : IOutboxHandler<DeadLetterTestEvent>
{
    public Task HandleAsync(DeadLetterTestEvent integrationEvent, CancellationToken cancellationToken)
        => Task.FromException(new InvalidOperationException("dead-letter-test-boom"));
}