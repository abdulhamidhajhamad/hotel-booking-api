using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Rooms.Abstractions;
using HotelBooking.Application.Features.Admin.Rooms.Common;

namespace HotelBooking.Application.Features.Admin.Rooms.GetById;

public sealed class GetRoomByIdQueryHandler : IQueryHandler<GetRoomByIdQuery, RoomDetail>
{
    private readonly IRoomReader _rooms;

    public GetRoomByIdQueryHandler(IRoomReader rooms) => _rooms = rooms;

    public async Task<Result<RoomDetail>> Handle(
        GetRoomByIdQuery query,
        CancellationToken cancellationToken)
    {
        var room = await _rooms.GetDetailAsync(query.Id, query.HotelId, cancellationToken);

        return room is null ? RoomErrors.NotFound(query.Id) : room;
    }
}
