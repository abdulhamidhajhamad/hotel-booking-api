using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Rooms.Common;

namespace HotelBooking.Application.Features.Admin.Rooms.Create;

public sealed record CreateRoomCommand(
    Guid HotelId,
    Guid RoomTypeId,
    string Number,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal PricePerNight,
    bool IsActive = true) : ICommand<RoomDetail>;