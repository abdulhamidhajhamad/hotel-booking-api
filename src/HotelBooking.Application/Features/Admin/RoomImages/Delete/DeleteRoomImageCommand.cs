using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Admin.RoomImages.Delete;

public sealed record DeleteRoomImageCommand(
    Guid HotelId,
    Guid RoomId,
    Guid ImageId) : ICommand;