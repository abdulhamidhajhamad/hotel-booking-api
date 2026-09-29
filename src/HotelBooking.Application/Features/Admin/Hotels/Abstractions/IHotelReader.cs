using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Features.Admin.Hotels.Common;
using HotelBooking.Application.Features.Admin.Hotels.GetList;

namespace HotelBooking.Application.Features.Admin.Hotels.Abstractions;

public interface IHotelReader
{
    Task<HotelDetail?> GetDetailAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<HotelGridItem>> GetPagedAsync(GetHotelsQuery query, CancellationToken cancellationToken);
}
