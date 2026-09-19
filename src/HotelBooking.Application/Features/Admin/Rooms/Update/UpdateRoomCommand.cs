using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Rooms.Common;

namespace HotelBooking.Application.Features.Admin.Rooms.Update;

public sealed record UpdateRoomCommand(
    Guid HotelId,
    Guid Id,
    Guid? RoomTypeId = null,
    string? Number = null,
    int? AdultsCapacity = null,
    int? ChildrenCapacity = null,
    decimal? PricePerNight = null,
    bool? IsActive = null) : ICommand<RoomDetail>;