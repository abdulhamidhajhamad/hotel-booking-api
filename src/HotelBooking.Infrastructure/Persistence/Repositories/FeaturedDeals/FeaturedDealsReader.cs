using HotelBooking.Application.Features.Hotels.GetFeaturedDeals;
using HotelBooking.Application.Features.Hotels.GetFeaturedDeals.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.FeaturedDeals;

public sealed class FeaturedDealsReader : IFeaturedDealsReader
{
    private readonly ApplicationDbContext _db;

    public FeaturedDealsReader(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<FeaturedDealDto>> GetFeaturedDealsAsync(int count, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var activeDeals = await _db.Discounts
            .AsNoTracking()
            .Where(d => d.StartUtc <= now && d.EndUtc >= now)
            .Where(d => d.Room.IsActive
                     && !d.Room.IsDeleted
                     && !d.Room.Hotel.IsDeleted)
            .Select(d => new
            {
                d.Room.HotelId,
                HotelName = d.Room.Hotel.Name,
                d.Room.Hotel.StarRating,
                CityName = d.Room.Hotel.City.Name,
                Country = d.Room.Hotel.City.Country,
                ThumbnailUrl = d.Room.Hotel.Images
                    .Where(i => !i.IsDeleted)
                    .OrderByDescending(i => i.IsPrimary)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                OriginalPrice = d.Room.PricePerNight,
                d.Percentage
            })
            .ToListAsync(cancellationToken);

        return activeDeals
            .GroupBy(d => d.HotelId)
            .Select(g => g.OrderByDescending(d => d.Percentage).First())
            .OrderByDescending(d => d.Percentage)
            .Take(count)
            .Select(d => new FeaturedDealDto(
                d.HotelId,
                d.HotelName,
                d.CityName,
                d.Country,
                d.StarRating,
                d.ThumbnailUrl,
                d.OriginalPrice,
                Math.Round(d.OriginalPrice * (1 - d.Percentage / 100m), 2),
                d.Percentage))
            .ToList();
    }
}
