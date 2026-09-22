namespace HotelBooking.Infrastructure.Storage.Options;

public sealed class CloudinaryOptions
{
    public string CloudName { get; init; } = default!;
    public string ApiKey { get; init; } = default!;
    public string ApiSecret { get; init; } = default!;
    public string DefaultFolder { get; init; } = "hotel-booking";
}