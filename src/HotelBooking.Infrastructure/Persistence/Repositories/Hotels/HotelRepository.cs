using HotelBooking.Application.Features.Admin.Hotels.Abstractions;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.Hotels;

public sealed class HotelRepository : IHotelRepository
{
    private readonly ApplicationDbContext _db;

    public HotelRepository(ApplicationDbContext db) => _db = db;

    public Task<HotelCitySummary?> GetCitySummaryAsync(Guid cityId, CancellationToken cancellationToken) =>
        _db.Cities
            .AsNoTracking()
            .Where(c => c.Id == cityId)
            .Select(c => new HotelCitySummary(c.Id, c.Name, c.Country))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<City?> GetCityAsync(Guid cityId, CancellationToken cancellationToken) =>
        _db.Cities.FirstOrDefaultAsync(c => c.Id == cityId, cancellationToken);

    public Task<Hotel?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Hotels.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public Task<Hotel?> GetByIdWithCityAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Hotels.Include(h => h.City).FirstOrDefaultAsync(h => h.Id == id, cancellationToken);

    public Task<int> CountRoomsAsync(Guid hotelId, CancellationToken cancellationToken) =>
        _db.Rooms.CountAsync(r => r.HotelId == hotelId, cancellationToken);

    public Task<string?> GetPrimaryImageUrlAsync(Guid hotelId, CancellationToken cancellationToken) =>
        _db.HotelImages
            .Where(i => i.HotelId == hotelId && i.IsPrimary)
            .Select(i => i.Url)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> HasActiveBookingsAsync(Guid hotelId, CancellationToken cancellationToken) =>
        _db.Bookings.AnyAsync(
            b => b.Room.HotelId == hotelId
                 && (b.Status == BookingStatus.Pending
                     || b.Status == BookingStatus.Confirmed
                     || b.Status == BookingStatus.CheckedIn),
            cancellationToken);

    public void Add(Hotel hotel) => _db.Hotels.Add(hotel);
}
