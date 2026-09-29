using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Features.Admin.Hotels.Abstractions;
using HotelBooking.Application.Features.Admin.Hotels.Common;
using HotelBooking.Application.Features.Admin.Hotels.GetList;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.Hotels;

public sealed class HotelReader : IHotelReader
{
    private readonly ApplicationDbContext _db;

    public HotelReader(ApplicationDbContext db) => _db = db;

    public Task<HotelDetail?> GetDetailAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Hotels
            .AsNoTracking()
            .Where(h => h.Id == id)
            .Select(h => new HotelDetail(
                h.Id,
                h.Name,
                h.Description,
                h.StarRating,
                h.Category,
                h.Address,
                h.Latitude,
                h.Longitude,
                h.CityId,
                h.City.Name,
                h.City.Country,
                h.OwnerName,
                h.Rooms.Count(r => !r.IsDeleted),
                h.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                h.CreatedAt,
                h.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<HotelGridItem>> GetPagedAsync(GetHotelsQuery query, CancellationToken cancellationToken)
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
