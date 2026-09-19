using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Rooms.Common;

namespace HotelBooking.Application.Features.Admin.Rooms.GetById;

public sealed record GetRoomByIdQuery(Guid HotelId, Guid Id) : IQuery<RoomDetail>;