using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HotelBooking.Application.Features.Cities.GetTrending;

public sealed class GetTrendingDestinationsQueryHandler
    : IQueryHandler<GetTrendingDestinationsQuery, IReadOnlyList<TrendingDestinationDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    private readonly IApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public GetTrendingDestinationsQueryHandler(IApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<Result<IReadOnlyList<TrendingDestinationDto>>> Handle(
        GetTrendingDestinationsQuery query,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"trending-destinations-{query.Count}";

        var destinations = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await LoadAsync(query.Count, cancellationToken);
        });

        return Result<IReadOnlyList<TrendingDestinationDto>>.Success(destinations!);
    }

    private async Task<IReadOnlyList<TrendingDestinationDto>> LoadAsync(
        int count,
        CancellationToken cancellationToken)
    {
        var ranked = await _db.Bookings
            .AsNoTracking()
            .Where(b => !b.IsDeleted && b.Status != BookingStatus.Cancelled)
            .GroupBy(b => b.Room.Hotel.CityId)
            .Select(g => new { CityId = g.Key, BookingCount = g.Count() })
            .OrderByDescending(x => x.BookingCount)
            .Take(count)
            .ToListAsync(cancellationToken);

        var cityIds = ranked.Select(x => x.CityId).ToList();

        var cities = await _db.Cities
            .AsNoTracking()
            .Where(c => cityIds.Contains(c.Id))
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Country,
                ThumbnailUrl = c.Images
                    .Where(i => !i.IsDeleted)
                    .OrderByDescending(i => i.IsPrimary)
                    .Select(i => i.Url)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return ranked
            .Join(cities, r => r.CityId, c => c.Id, (r, c) => new TrendingDestinationDto(
                c.Id, c.Name, c.Country, c.ThumbnailUrl, r.BookingCount))
            .ToList();
    }
}