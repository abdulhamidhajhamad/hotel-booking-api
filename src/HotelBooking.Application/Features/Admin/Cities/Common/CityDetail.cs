namespace HotelBooking.Application.Features.Admin.Cities.Common;

public sealed record CityDetail(
    Guid Id,
    string Name,
    string Country,
    string? PostalCode,
    string Timezone,
    int NumberOfHotels,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);