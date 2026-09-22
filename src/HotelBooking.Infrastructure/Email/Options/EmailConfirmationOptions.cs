namespace HotelBooking.Infrastructure.Email.Options;

public sealed class EmailConfirmationOptions
{
    public int TokenLifetimeHours { get; init; } = 24;
    public int TokenByteLength { get; init; } = 32;
    public string ConfirmUrlTemplate { get; init; } = default!;
    public int ResendCooldownSeconds { get; init; } = 60;
}
