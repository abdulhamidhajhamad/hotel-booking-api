namespace HotelBooking.Application.Features.Admin.Rooms.Common;

public sealed record RoomDetail(
    Guid Id,
    Guid HotelId,
    string HotelName,
    Guid RoomTypeId,
    string RoomTypeName,
    string Number,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal PricePerNight,
    bool IsActive,
    int NumberOfImages,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);