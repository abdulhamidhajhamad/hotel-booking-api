namespace HotelBooking.Application.Features.Admin.Cities.Common;

public sealed record CityGridItem(
    Guid Id,
    string Name,
    string Country,
    string? PostalCode,
    int NumberOfHotels,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);