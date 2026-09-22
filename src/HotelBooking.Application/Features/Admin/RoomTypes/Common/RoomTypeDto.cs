namespace HotelBooking.Application.Features.Admin.RoomTypes.Common;

public sealed record RoomTypeDto(
    Guid Id,
    string Name,
    string? Description,
    int NumberOfRooms,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);