using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Reviews.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Reviews.GetForHotel;

public sealed class GetHotelReviewsQueryHandler
    : IQueryHandler<GetHotelReviewsQuery, PagedResult<ReviewDto>>
{
    private readonly IApplicationDbContext _db;

    public GetHotelReviewsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<PagedResult<ReviewDto>>> Handle(
        GetHotelReviewsQuery query,
        CancellationToken cancellationToken)
    {
        var source = _db.Reviews
            .AsNoTracking()
            .Where(r => r.HotelId == query.HotelId);

        var totalCount = await source.CountAsync(cancellationToken);

        var items = await source
            .OrderByDescending(r => r.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new ReviewDto(
                r.Id,
                r.HotelId,
                r.Rating,
                r.Comment,
                r.User.FullName,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return PagedResult<ReviewDto>.Create(items, query.Page, query.PageSize, totalCount);
    }
}