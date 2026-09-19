using HotelBooking.Domain.Common;

namespace HotelBooking.Application.Features.Admin.Hotels.Common;

public sealed record HotelGridItem(
    Guid Id,
    string Name,
    int StarRating,
    HotelCategory Category,
    string CityName,
    string? OwnerFullName,
    int NumberOfRooms,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);