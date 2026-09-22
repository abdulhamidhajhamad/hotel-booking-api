using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Delete;

public sealed record DeleteRoomTypeCommand(Guid Id) : ICommand;