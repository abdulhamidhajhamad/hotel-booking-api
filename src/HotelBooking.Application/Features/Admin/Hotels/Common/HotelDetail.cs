using HotelBooking.Domain.Common;

namespace HotelBooking.Application.Features.Admin.Hotels.Common;

public sealed record HotelDetail(
    Guid Id,
    string Name,
    string? Description,
    int StarRating,
    HotelCategory Category,
    string Address,
    double? Latitude,
    double? Longitude,
    Guid CityId,
    string CityName,
    string CityCountry,
    string? OwnerName,
    int NumberOfRooms,
    string? PrimaryImageUrl,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);