using HotelBooking.Application.Features.Admin.RoomTypes.Abstractions;
using HotelBooking.Application.Features.Admin.RoomTypes.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.RoomTypes;

public sealed class RoomTypeReader : IRoomTypeReader
{
    private readonly ApplicationDbContext _db;

    public RoomTypeReader(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<RoomTypeDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _db.RoomTypes
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new RoomTypeDto(
                t.Id,
                t.Name,
                t.Description,
                t.Rooms.Count(r => !r.IsDeleted),
                t.CreatedAt,
                t.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
