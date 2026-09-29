using HotelBooking.Application.Common.Pagination;

namespace HotelBooking.Application.Features.Hotels.Search.Abstractions;

public interface IHotelSearchReader
{
    Task<PagedResult<HotelSearchResultDto>> SearchAsync(SearchHotelsQuery query, CancellationToken cancellationToken);
}
