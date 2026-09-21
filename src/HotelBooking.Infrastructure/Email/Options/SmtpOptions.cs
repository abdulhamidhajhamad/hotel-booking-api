namespace HotelBooking.Infrastructure.Email.Options;

public sealed class SmtpOptions
{
    public string Host { get; init; } = default!;
    public int Port { get; init; }
    public string? Username { get; init; }
    public string? Password { get; init; }
    public bool UseSsl { get; init; }
    public string FromEmail { get; init; } = default!;
    public string FromName { get; init; } = "Hotel Booking";
}