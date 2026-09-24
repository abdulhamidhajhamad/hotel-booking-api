namespace HotelBooking.Application.Abstractions.Payments;

public interface IPaymentGateway
{
    Task<PaymentIntentCreated> CreateIntentAsync(
        CreatePaymentIntentRequest request,
        CancellationToken cancellationToken);

    Task<PaymentConfirmation> ConfirmIntentAsync(
        ConfirmPaymentIntentRequest request,
        CancellationToken cancellationToken);
}