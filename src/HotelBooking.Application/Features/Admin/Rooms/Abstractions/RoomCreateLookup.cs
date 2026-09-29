namespace HotelBooking.Application.Features.Admin.Rooms.Abstractions;

public sealed record RoomTypeRef(Guid Id, string Name);

public sealed record RoomCreateLookup(string HotelName, RoomTypeRef? RoomType, bool DuplicateNumber);
