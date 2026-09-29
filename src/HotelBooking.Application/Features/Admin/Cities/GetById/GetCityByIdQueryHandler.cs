using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Cities.Abstractions;
using HotelBooking.Application.Features.Admin.Cities.Common;

namespace HotelBooking.Application.Features.Admin.Cities.GetById;

public sealed class GetCityByIdQueryHandler
    : IQueryHandler<GetCityByIdQuery, CityDetail>
{
    private readonly ICityReader _cities;

    public GetCityByIdQueryHandler(ICityReader cities) => _cities = cities;

    public async Task<Result<CityDetail>> Handle(
        GetCityByIdQuery query,
        CancellationToken cancellationToken)
    {
        var city = await _cities.GetDetailAsync(query.Id, cancellationToken);

        return city is null
            ? CityErrors.NotFound(query.Id)
            : city;
    }
}
