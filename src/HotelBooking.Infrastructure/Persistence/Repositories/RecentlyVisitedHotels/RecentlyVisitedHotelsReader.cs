using HotelBooking.Application.Features.Hotels.RecentlyVisited;
using HotelBooking.Application.Features.Hotels.RecentlyVisited.Abstractions;
using HotelBooking.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.RecentlyVisitedHotels;

public sealed class RecentlyVisitedHotelsReader : IRecentlyVisitedHotelsReader
{
    private readonly ApplicationDbContext _db;

    public RecentlyVisitedHotelsReader(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<RecentlyVisitedHotelDto>> GetRecentlyVisitedAsync(Guid userId, int count, CancellationToken cancellationToken)
    {
        var bookings = await _db.Bookings
            .AsNoTracking()
            .Where(b => !b.IsDeleted
                     && b.Status != BookingStatus.Cancelled
                     && b.BookingGroup.UserId == userId)
            .Select(b => new
            {
                b.Room.HotelId,
                HotelName = b.Room.Hotel.Name,
                CityName = b.Room.Hotel.City.Name,
                Country = b.Room.Hotel.City.Country,
                b.Room.Hotel.StarRating,
                ThumbnailUrl = b.Room.Hotel.Images
                    .Where(i => !i.IsDeleted)
                    .OrderByDescending(i => i.IsPrimary)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                b.Room.PricePerNight,
                b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return bookings
            .GroupBy(b => b.HotelId)
            .Select(g => g.OrderByDescending(b => b.CreatedAt).First())
            .OrderByDescending(b => b.CreatedAt)
            .Take(count)
            .Select(b => new RecentlyVisitedHotelDto(
                b.HotelId,
                b.HotelName,
                b.CityName,
                b.Country,
                b.StarRating,
                b.ThumbnailUrl,
                Math.Round(b.PricePerNight, 2),
                b.CreatedAt))
            .ToList();
    }
}
