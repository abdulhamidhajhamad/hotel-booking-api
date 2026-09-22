using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Cities.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Cities.GetById;

public sealed class GetCityByIdQueryHandler
    : IQueryHandler<GetCityByIdQuery, CityDetail>
{
    private readonly IApplicationDbContext _db;

    public GetCityByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<CityDetail>> Handle(
        GetCityByIdQuery query,
        CancellationToken cancellationToken)
    {
        var city = await _db.Cities
            .AsNoTracking()
            .Where(c => c.Id == query.Id)
            .Select(c => new CityDetail(
                c.Id,
                c.Name,
                c.Country,
                c.PostalCode,
                c.Timezone,
                c.Hotels.Count(h => !h.IsDeleted),
                c.CreatedAt,
                c.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return city is null
            ? CityErrors.NotFound(query.Id)
            : city;
    }
}