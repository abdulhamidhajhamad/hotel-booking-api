using HotelBooking.Application.Features.Cities.GetTrending;
using HotelBooking.Application.Features.Cities.GetTrending.Abstractions;
using HotelBooking.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.TrendingDestinations;

public sealed class TrendingDestinationsReader : ITrendingDestinationsReader
{
    private readonly ApplicationDbContext _db;

    public TrendingDestinationsReader(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<TrendingDestinationDto>> GetTrendingAsync(int count, CancellationToken cancellationToken)
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
