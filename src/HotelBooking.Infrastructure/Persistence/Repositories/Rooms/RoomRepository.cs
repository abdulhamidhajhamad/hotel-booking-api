using HotelBooking.Application.Features.Admin.Rooms.Abstractions;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.Rooms;

public sealed class RoomRepository : IRoomRepository
{
    private readonly ApplicationDbContext _db;

    public RoomRepository(ApplicationDbContext db) => _db = db;

    public Task<RoomCreateLookup?> GetCreateLookupAsync(Guid hotelId, Guid roomTypeId, string number, CancellationToken cancellationToken) =>
        _db.Hotels
            .Where(h => h.Id == hotelId)
            .Select(h => new RoomCreateLookup(
                h.Name,
                _db.RoomTypes
                    .Where(t => t.Id == roomTypeId)
                    .Select(t => new RoomTypeRef(t.Id, t.Name))
                    .FirstOrDefault(),
                _db.Rooms.Any(r => r.HotelId == hotelId && r.Number == number)))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<Room?> GetByIdAsync(Guid id, Guid hotelId, CancellationToken cancellationToken) =>
        _db.Rooms.FirstOrDefaultAsync(r => r.Id == id && r.HotelId == hotelId, cancellationToken);

    public Task<Room?> GetByIdWithRelationsAsync(Guid id, Guid hotelId, CancellationToken cancellationToken) =>
        _db.Rooms
            .Include(r => r.Hotel)
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(r => r.Id == id && r.HotelId == hotelId, cancellationToken);

    public Task<RoomType?> GetRoomTypeAsync(Guid roomTypeId, CancellationToken cancellationToken) =>
        _db.RoomTypes.FirstOrDefaultAsync(t => t.Id == roomTypeId, cancellationToken);

    public Task<bool> HasDuplicateNumberAsync(Guid hotelId, Guid excludeRoomId, string number, CancellationToken cancellationToken) =>
        _db.Rooms.AnyAsync(
            r => r.HotelId == hotelId && r.Id != excludeRoomId && r.Number == number,
            cancellationToken);

    public Task<int> CountImagesAsync(Guid roomId, CancellationToken cancellationToken) =>
        _db.RoomImages.CountAsync(i => i.RoomId == roomId, cancellationToken);

    public Task<bool> HasActiveBookingsAsync(Guid roomId, CancellationToken cancellationToken) =>
        _db.Bookings.AnyAsync(
            b => b.RoomId == roomId
                 && (b.Status == BookingStatus.Pending
                     || b.Status == BookingStatus.Confirmed
                     || b.Status == BookingStatus.CheckedIn),
            cancellationToken);

    public void Add(Room room) => _db.Rooms.Add(room);
}
