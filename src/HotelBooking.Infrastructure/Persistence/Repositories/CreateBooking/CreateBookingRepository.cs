using HotelBooking.Application.Features.Bookings.Create.Abstractions;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.CreateBooking;

public sealed class CreateBookingRepository : ICreateBookingRepository
{
    private readonly ApplicationDbContext _db;

    public CreateBookingRepository(ApplicationDbContext db) => _db = db;

    public Task<IdempotencyRecord?> GetIdempotencyRecordAsync(string key, CancellationToken cancellationToken) =>
        _db.IdempotencyRecords.FirstOrDefaultAsync(r => r.Key == key, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, RoomPricing>> GetRoomPricingAsync(
        IReadOnlyCollection<Guid> roomIds, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        return await _db.Rooms
            .Where(r => roomIds.Contains(r.Id))
            .Select(r => new RoomPricing(
                r.Id,
                r.IsActive,
                r.AdultsCapacity,
                r.ChildrenCapacity,
                r.PricePerNight,
                r.Discounts
                    .Where(d => d.StartUtc <= now && d.EndUtc >= now)
                    .OrderByDescending(d => d.Percentage)
                    .Select(d => (decimal?)d.Percentage)
                    .FirstOrDefault()))
            .ToDictionaryAsync(r => r.RoomId, cancellationToken);
    }

    public async Task<IReadOnlyList<(Guid RoomId, DateOnly Date)>> GetTakenSlotsAsync(
        IReadOnlyCollection<Guid> roomIds, CancellationToken cancellationToken)
    {
        var slots = await _db.RoomAvailability
            .Where(a => roomIds.Contains(a.RoomId))
            .Select(a => new { a.RoomId, a.Date })
            .ToListAsync(cancellationToken);

        return slots.Select(a => (a.RoomId, a.Date)).ToList();
    }

    public async Task<bool> TryPersistBookingAsync(
        BookingGroup group, IdempotencyRecord record, CancellationToken cancellationToken)
    {
        _db.BookingGroups.Add(group);
        _db.IdempotencyRecords.Add(record);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public Task<BookingGroupSnapshot?> GetBookingGroupSnapshotAsync(
        Guid bookingGroupId, CancellationToken cancellationToken) =>
        _db.BookingGroups
            .AsNoTracking()
            .Where(g => g.Id == bookingGroupId)
            .Select(g => new BookingGroupSnapshot(g.Id, g.ConfirmationNumber, g.TotalPrice, g.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);
}
