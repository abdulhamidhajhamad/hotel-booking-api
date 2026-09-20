using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Hotels.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Hotels.GetList;

public sealed class GetHotelsQueryHandler
    : IQueryHandler<GetHotelsQuery, PagedResult<HotelGridItem>>
{
    private readonly IApplicationDbContext _db;

    public GetHotelsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<PagedResult<HotelGridItem>>> Handle(
        GetHotelsQuery query,
        CancellationToken cancellationToken)
    {
        var source = _db.Hotels.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(h => h.Name.Contains(search));
        }

        if (query.CityId.HasValue)
            source = source.Where(h => h.CityId == query.CityId.Value);

        if (query.MinStar.HasValue)
            source = source.Where(h => h.StarRating >= query.MinStar.Value);

        if (query.Category.HasValue)
            source = source.Where(h => h.Category == query.Category.Value);

        var totalCount = await source.CountAsync(cancellationToken);

        source = (query.SortBy?.ToLowerInvariant()) switch
        {
            "starrating" => query.SortDesc ? source.OrderByDescending(h => h.StarRating) : source.OrderBy(h => h.StarRating),
            "createdat"  => query.SortDesc ? source.OrderByDescending(h => h.CreatedAt)  : source.OrderBy(h => h.CreatedAt),
            _            => query.SortDesc ? source.OrderByDescending(h => h.Name)       : source.OrderBy(h => h.Name),
        };

        var items = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(h => new HotelGridItem(
                h.Id,
                h.Name,
                h.StarRating,
                h.Category,
                h.City.Name,
                h.OwnerName,
                h.Rooms.Count(r => !r.IsDeleted),
                h.CreatedAt,
                h.UpdatedAt))
            .ToListAsync(cancellationToken);

        return PagedResult<HotelGridItem>.Create(items, query.Page, query.PageSize, totalCount);
    }
}