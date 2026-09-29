using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Features.Admin.Rooms.Common;
using HotelBooking.Application.Features.Admin.Rooms.GetList;

namespace HotelBooking.Application.Features.Admin.Rooms.Abstractions;

public interface IRoomReader
{
    Task<RoomDetail?> GetDetailAsync(Guid id, Guid hotelId, CancellationToken cancellationToken);

    Task<PagedResult<RoomGridItem>> GetPagedAsync(GetRoomsQuery query, CancellationToken cancellationToken);
}
