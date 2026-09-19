using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Admin.Rooms.Delete;

public sealed record DeleteRoomCommand(Guid HotelId, Guid Id) : ICommand;