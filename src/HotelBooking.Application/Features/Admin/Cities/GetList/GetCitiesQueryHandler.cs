using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Cities.Abstractions;
using HotelBooking.Application.Features.Admin.Cities.Common;

namespace HotelBooking.Application.Features.Admin.Cities.GetList;

public sealed class GetCitiesQueryHandler
    : IQueryHandler<GetCitiesQuery, PagedResult<CityGridItem>>
{
    private readonly ICityReader _cities;

    public GetCitiesQueryHandler(ICityReader cities) => _cities = cities;

    public async Task<Result<PagedResult<CityGridItem>>> Handle(
        GetCitiesQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _cities.GetPagedAsync(query, cancellationToken);

        return result;
    }
}
