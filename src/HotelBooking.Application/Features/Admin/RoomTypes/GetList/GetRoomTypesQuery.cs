using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.RoomTypes.Common;

namespace HotelBooking.Application.Features.Admin.RoomTypes.GetList;

public sealed record GetRoomTypesQuery : IQuery<IReadOnlyList<RoomTypeDto>>;