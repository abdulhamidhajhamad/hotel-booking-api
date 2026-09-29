using HotelBooking.Application.Features.Admin.RoomImages.Abstractions;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.RoomImages;

public sealed class RoomImageRepository : IRoomImageRepository
{
    private readonly ApplicationDbContext _db;

    public RoomImageRepository(ApplicationDbContext db) => _db = db;

    public Task<bool> RoomExistsAsync(Guid roomId, Guid hotelId, CancellationToken cancellationToken) =>
        _db.Rooms.AnyAsync(r => r.Id == roomId && r.HotelId == hotelId, cancellationToken);

    public Task<RoomImage?> GetByIdAsync(Guid imageId, Guid roomId, Guid hotelId, CancellationToken cancellationToken) =>
        _db.RoomImages.FirstOrDefaultAsync(
            i => i.Id == imageId && i.RoomId == roomId && i.Room.HotelId == hotelId,
            cancellationToken);

    public void Add(RoomImage image) => _db.RoomImages.Add(image);

    public void Remove(RoomImage image) => _db.RoomImages.Remove(image);
}
