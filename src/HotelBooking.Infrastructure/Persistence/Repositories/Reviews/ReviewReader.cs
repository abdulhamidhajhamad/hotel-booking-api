using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Features.Reviews.Abstractions;
using HotelBooking.Application.Features.Reviews.Common;
using HotelBooking.Application.Features.Reviews.GetForHotel;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.Reviews;

public sealed class ReviewReader : IReviewReader
{
    private readonly ApplicationDbContext _db;

    public ReviewReader(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<ReviewDto>> GetForHotelAsync(GetHotelReviewsQuery query, CancellationToken cancellationToken)
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
