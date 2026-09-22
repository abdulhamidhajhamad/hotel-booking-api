using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Rooms.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Rooms.GetList;

public sealed class GetRoomsQueryHandler
    : IQueryHandler<GetRoomsQuery, PagedResult<RoomGridItem>>
{
    private readonly IApplicationDbContext _db;

    public GetRoomsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<PagedResult<RoomGridItem>>> Handle(
        GetRoomsQuery query,
        CancellationToken cancellationToken)
    {
        var source = _db.Rooms.AsNoTracking().Where(r => r.HotelId == query.HotelId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            source = source.Where(r => r.Number.Contains(search));
        }

        if (query.RoomTypeId.HasValue)
            source = source.Where(r => r.RoomTypeId == query.RoomTypeId.Value);

        if (query.MinAdults.HasValue)
            source = source.Where(r => r.AdultsCapacity >= query.MinAdults.Value);

        if (query.MinChildren.HasValue)
            source = source.Where(r => r.ChildrenCapacity >= query.MinChildren.Value);

        if (query.IsActive.HasValue)
            source = source.Where(r => r.IsActive == query.IsActive.Value);

        var totalCount = await source.CountAsync(cancellationToken);

        source = (query.SortBy?.ToLowerInvariant()) switch
        {
            "price"     => query.SortDesc ? source.OrderByDescending(r => r.PricePerNight)    : source.OrderBy(r => r.PricePerNight),
            "adults"    => query.SortDesc ? source.OrderByDescending(r => r.AdultsCapacity)   : source.OrderBy(r => r.AdultsCapacity),
            "children"  => query.SortDesc ? source.OrderByDescending(r => r.ChildrenCapacity) : source.OrderBy(r => r.ChildrenCapacity),
            "createdat" => query.SortDesc ? source.OrderByDescending(r => r.CreatedAt)        : source.OrderBy(r => r.CreatedAt),
            _           => query.SortDesc ? source.OrderByDescending(r => r.Number)           : source.OrderBy(r => r.Number),
        };

        var items = await source
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new RoomGridItem(
                r.Id,
                r.Number,
                r.RoomType.Name,
                r.AdultsCapacity,
                r.ChildrenCapacity,
                r.PricePerNight,
                r.IsActive,
                r.CreatedAt,
                r.UpdatedAt))
            .ToListAsync(cancellationToken);

        return PagedResult<RoomGridItem>.Create(items, query.Page, query.PageSize, totalCount);
    }
}