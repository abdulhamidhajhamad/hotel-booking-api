using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Cities.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Cities.GetList;

public sealed class GetCitiesQueryHandler
    : IQueryHandler<GetCitiesQuery, PagedResult<CityGridItem>>
{
    private readonly IApplicationDbContext _db;

    public GetCitiesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<PagedResult<CityGridItem>>> Handle(
        GetCitiesQuery query,
        CancellationToken cancellationToken)
    {
        var source = _db.Cities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            var country = search.ToUpperInvariant();
            source = source.Where(c => c.Name.Contains(search) || c.Country == country);
        }

        var totalCount = await source.CountAsync(cancellationToken);

        source = (query.SortBy?.ToLowerInvariant()) switch
        {
            "country"   => query.SortDesc ? source.OrderByDescending(c => c.Country)   : source.OrderBy(c => c.Country),
            "createdat" => query.SortDesc ? source.OrderByDescending(c => c.CreatedAt) : source.OrderBy(c => c.CreatedAt),
            _           => query.SortDesc ? source.OrderByDescending(c => c.Name)      : source.OrderBy(c => c.Name),
        };

        var items = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(c => new CityGridItem(
                c.Id,
                c.Name,
                c.Country,
                c.PostalCode,
                c.Hotels.Count(h => !h.IsDeleted),
                c.CreatedAt,
                c.UpdatedAt))
            .ToListAsync(cancellationToken);

        return PagedResult<CityGridItem>.Create(items, query.Page, query.PageSize, totalCount);
    }
}