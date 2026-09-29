using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Rooms.Abstractions;
using HotelBooking.Application.Features.Admin.Rooms.Common;

namespace HotelBooking.Application.Features.Admin.Rooms.GetList;

public sealed class GetRoomsQueryHandler
    : IQueryHandler<GetRoomsQuery, PagedResult<RoomGridItem>>
{
    private readonly IRoomReader _rooms;

    public GetRoomsQueryHandler(IRoomReader rooms) => _rooms = rooms;

    public async Task<Result<PagedResult<RoomGridItem>>> Handle(
        GetRoomsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _rooms.GetPagedAsync(query, cancellationToken);

        return result;
    }
}
