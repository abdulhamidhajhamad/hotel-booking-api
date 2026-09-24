namespace HotelBooking.Infrastructure.Payments.Options;

public sealed class StripeOptions
{
    public string SecretKey { get; set; } = default!;
    public string Currency { get; set; } = "USD";
}