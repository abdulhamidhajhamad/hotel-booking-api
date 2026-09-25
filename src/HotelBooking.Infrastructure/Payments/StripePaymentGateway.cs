using HotelBooking.Application.Abstractions.Payments;
using HotelBooking.Infrastructure.Payments.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;

namespace HotelBooking.Infrastructure.Payments;

public sealed class StripePaymentGateway : IPaymentGateway
{
    private readonly PaymentIntentService _paymentIntents;
    private readonly ILogger<StripePaymentGateway> _logger;

    public StripePaymentGateway(IOptions<StripeOptions> options, ILogger<StripePaymentGateway> logger)
    {
        _paymentIntents = new PaymentIntentService(new StripeClient(options.Value.SecretKey));
        _logger = logger;
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

        _logger.LogInformation(
            "Payment intent {IntentId} created for {Amount} {Currency}",
            intent.Id, request.Amount, request.Currency);

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
            _logger.LogWarning(
                "Payment confirmation failed for intent {IntentId}: {StripeError}",
                request.PaymentIntentId, ex.StripeError?.Message ?? ex.Message);

            return new PaymentConfirmation(false, request.PaymentIntentId, ex.StripeError?.Message ?? ex.Message);
        }
    }

    private static long ToMinorUnits(decimal amount) =>
        (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}