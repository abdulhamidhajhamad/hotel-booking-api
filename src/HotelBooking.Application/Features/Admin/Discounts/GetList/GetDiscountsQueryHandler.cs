using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Discounts.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Discounts.GetList;

public sealed class GetDiscountsQueryHandler
    : IQueryHandler<GetDiscountsQuery, PagedResult<DiscountDto>>
{
    private readonly IApplicationDbContext _db;

    public GetDiscountsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<PagedResult<DiscountDto>>> Handle(
        GetDiscountsQuery query,
        CancellationToken cancellationToken)
    {
        var source = _db.Discounts.AsNoTracking().IgnoreQueryFilters();

        if (query.RoomId.HasValue)
            source = source.Where(d => d.RoomId == query.RoomId.Value);

        if (query.HotelId.HasValue)
            source = source.Where(d => d.Room.HotelId == query.HotelId.Value);

        var now = DateTime.UtcNow;
        if (query.ActiveOnly == true)
            source = source.Where(d => d.StartUtc <= now && d.EndUtc >= now);

        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
            .OrderByDescending(d => d.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(d => new DiscountDto(
                d.Id,
                d.RoomId,
                d.Room.Number,
                d.Room.HotelId,
                d.Room.Hotel.Name,
                d.Title,
                d.Percentage,
                d.StartUtc,
                d.EndUtc,
                d.StartUtc <= now && d.EndUtc >= now,
                d.CreatedAt))
            .ToListAsync(cancellationToken);

        return PagedResult<DiscountDto>.Create(items, query.Page, query.PageSize, totalCount);
    }
}