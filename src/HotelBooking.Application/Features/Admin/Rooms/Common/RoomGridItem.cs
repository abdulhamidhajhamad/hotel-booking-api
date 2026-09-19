namespace HotelBooking.Application.Features.Admin.Rooms.Common;

public sealed record RoomGridItem(
    Guid Id,
    string Number,
    string RoomTypeName,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal PricePerNight,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);