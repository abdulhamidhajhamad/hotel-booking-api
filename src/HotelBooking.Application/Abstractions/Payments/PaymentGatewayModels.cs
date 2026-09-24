namespace HotelBooking.Application.Abstractions.Payments;

public sealed record CreatePaymentIntentRequest(
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    string Description);

public sealed record ConfirmPaymentIntentRequest(
    string PaymentIntentId,
    string PaymentMethodId,
    string IdempotencyKey);

public sealed record PaymentIntentCreated(string PaymentIntentId);

public sealed record PaymentConfirmation(
    bool Succeeded,
    string PaymentIntentId,
    string? FailureReason);