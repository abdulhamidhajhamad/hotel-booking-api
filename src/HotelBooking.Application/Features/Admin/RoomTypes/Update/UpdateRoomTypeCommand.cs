using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.RoomTypes.Common;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Update;

public sealed record UpdateRoomTypeCommand(
    Guid Id,
    string? Name = null,
    string? Description = null) : ICommand<RoomTypeDto>;