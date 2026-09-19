namespace HotelBooking.Application.Features.Admin.Amenities.Common;

public sealed record AmenityDto(
    Guid Id,
    string Name,
    string? Icon,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);