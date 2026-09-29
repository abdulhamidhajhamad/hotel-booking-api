using HotelBooking.Application.Features.Admin.RoomTypes.Abstractions;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.RoomTypes;

public sealed class RoomTypeRepository : IRoomTypeRepository
{
    private readonly ApplicationDbContext _db;

    public RoomTypeRepository(ApplicationDbContext db) => _db = db;

    public Task<RoomType?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.RoomTypes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

    public Task<string?> GetExistingNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken)
    {
        var normalized = name.ToLower();

        return _db.RoomTypes
            .Where(t => t.Name.ToLower() == normalized && (excludeId == null || t.Id != excludeId))
            .Select(t => t.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<int> CountRoomsAsync(Guid roomTypeId, CancellationToken cancellationToken) =>
        _db.Rooms.CountAsync(r => r.RoomTypeId == roomTypeId, cancellationToken);

    public Task<bool> HasRoomsAsync(Guid roomTypeId, CancellationToken cancellationToken) =>
        _db.Rooms.AnyAsync(r => r.RoomTypeId == roomTypeId, cancellationToken);

    public void Add(RoomType roomType) => _db.RoomTypes.Add(roomType);
}
