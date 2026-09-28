using HotelBooking.Domain.Common;

namespace HotelBooking.Application.Features.Hotels.GetDetails;

public sealed record HotelDetailsDto(
    Guid HotelId,
    string HotelName,
    string? Description,
    int StarRating,
    HotelCategory Category,
    string Address,
    double? Latitude,
    double? Longitude,
    string CityName,
    string Country,
    string? OwnerName,
    IReadOnlyList<HotelImageDto> Images,
    IReadOnlyList<HotelAmenityDto> Amenities,
    IReadOnlyList<HotelRoomDto> Rooms);

public sealed record HotelImageDto(
    Guid Id,
    string Url,
    bool IsPrimary);

public sealed record HotelAmenityDto(
    Guid Id,
    string Name,
    string? Icon);

public sealed record HotelRoomDto(
    Guid RoomId,
    string Number,
    string RoomTypeName,
    string? RoomTypeDescription,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal OriginalPricePerNight,
    decimal DiscountedPricePerNight,
    string? ThumbnailUrl);