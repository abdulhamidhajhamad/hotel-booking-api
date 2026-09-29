using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Features.Admin.Cities.Common;
using HotelBooking.Application.Features.Admin.Cities.GetList;

namespace HotelBooking.Application.Features.Admin.Cities.Abstractions;

public interface ICityReader
{
    Task<CityDetail?> GetDetailAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<CityGridItem>> GetPagedAsync(GetCitiesQuery query, CancellationToken cancellationToken);
}
