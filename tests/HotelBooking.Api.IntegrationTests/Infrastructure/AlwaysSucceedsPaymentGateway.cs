using HotelBooking.Application.Abstractions.Payments;

namespace HotelBooking.Api.IntegrationTests.Infrastructure;

public sealed class AlwaysSucceedsPaymentGateway : IPaymentGateway
{
    public Task<PaymentIntentCreated> CreateIntentAsync(
        CreatePaymentIntentRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new PaymentIntentCreated($"pi_{Guid.NewGuid():N}"));

    public Task<PaymentConfirmation> ConfirmIntentAsync(
        ConfirmPaymentIntentRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new PaymentConfirmation(true, request.PaymentIntentId, null));
}