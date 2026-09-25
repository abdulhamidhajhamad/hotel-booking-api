using HotelBooking.Application.Abstractions.Payments;
using HotelBooking.Infrastructure.Payments.Options;
using Microsoft.Extensions.Options;
using Stripe;

namespace HotelBooking.Infrastructure.Payments;

public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly PaymentIntentService _paymentIntents;

    public StripePaymentGateway(IOptions<StripeOptions> options)
    {
        _paymentIntents = new PaymentIntentService(new StripeClient(options.Value.SecretKey));
    }

    public async Task<PaymentIntentCreated> CreateIntentAsync(
        CreatePaymentIntentRequest request,
        CancellationToken cancellationToken)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = ToMinorUnits(request.Amount),
            Currency = request.Currency.ToLowerInvariant(),
            Description = request.Description,
            CaptureMethod = "automatic",
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true,
                AllowRedirects = "never"
            }
        };

        var requestOptions = new RequestOptions { IdempotencyKey = $"{request.IdempotencyKey}:create" };

        var intent = await _paymentIntents.CreateAsync(options, requestOptions, cancellationToken);
        return new PaymentIntentCreated(intent.Id);
    }

    public async Task<PaymentConfirmation> ConfirmIntentAsync(
        ConfirmPaymentIntentRequest request,
        CancellationToken cancellationToken)
    {
        var options = new PaymentIntentConfirmOptions
        {
            PaymentMethod = request.PaymentMethodId
        };

        var requestOptions = new RequestOptions { IdempotencyKey = $"{request.IdempotencyKey}:confirm" };

        try
        {
            var intent = await _paymentIntents.ConfirmAsync(
                request.PaymentIntentId, options, requestOptions, cancellationToken);

            var succeeded = intent.Status == "succeeded";
            return new PaymentConfirmation(succeeded, intent.Id, succeeded ? null : intent.Status);
        }
        catch (StripeException ex)
        {
            return new PaymentConfirmation(false, request.PaymentIntentId, ex.StripeError?.Message ?? ex.Message);
        }
    }

    private static long ToMinorUnits(decimal amount) =>
        (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}