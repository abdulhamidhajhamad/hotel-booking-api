using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.RoomTypes.Common;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Create;

public sealed record CreateRoomTypeCommand(
    string Name,
    string? Description) : ICommand<RoomTypeDto>;