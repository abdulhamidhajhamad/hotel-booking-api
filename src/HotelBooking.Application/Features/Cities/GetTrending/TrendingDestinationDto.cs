namespace HotelBooking.Application.Features.Cities.GetTrending;

public sealed record TrendingDestinationDto(
    Guid CityId,
    string Name,
    string Country,
    string? ThumbnailUrl,
    int BookingCount);