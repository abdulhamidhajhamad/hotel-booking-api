using HotelBooking.Application.Abstractions.Payments;

namespace HotelBooking.Application.UnitTests.Common.Fakes;

public sealed class FakePaymentGateway : IPaymentGateway
{
    public bool ConfirmSucceeds { get; set; } = true;
    public string? FailureReason { get; set; } = "card_declined";
    public int CreateCallCount { get; private set; }
    public int ConfirmCallCount { get; private set; }

    public Task<PaymentIntentCreated> CreateIntentAsync(
        CreatePaymentIntentRequest request,
        CancellationToken cancellationToken)
    {
        CreateCallCount++;
        return Task.FromResult(new PaymentIntentCreated($"pi_{CreateCallCount}"));
    }

    public Task<PaymentConfirmation> ConfirmIntentAsync(
        ConfirmPaymentIntentRequest request,
        CancellationToken cancellationToken)
    {
        ConfirmCallCount++;
        return Task.FromResult(ConfirmSucceeds
            ? new PaymentConfirmation(true, request.PaymentIntentId, null)
            : new PaymentConfirmation(false, request.PaymentIntentId, FailureReason));
    }
}