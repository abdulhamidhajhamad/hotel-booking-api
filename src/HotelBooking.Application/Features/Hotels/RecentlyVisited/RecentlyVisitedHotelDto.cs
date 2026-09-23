namespace HotelBooking.Application.Features.Hotels.RecentlyVisited;

public sealed record RecentlyVisitedHotelDto(
    Guid HotelId,
    string HotelName,
    string CityName,
    string Country,
    int StarRating,
    string? ThumbnailUrl,
    decimal PricePerNight,
    DateTimeOffset LastBookedAt);