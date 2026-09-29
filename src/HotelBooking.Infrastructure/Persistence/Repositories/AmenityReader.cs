using HotelBooking.Application.Features.Admin.Amenities.Abstractions;
using HotelBooking.Application.Features.Admin.Amenities.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class AmenityReader : IAmenityReader
{
    private readonly ApplicationDbContext _db;

    public AmenityReader(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<AmenityDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _db.Amenities
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new AmenityDto(
                a.Id,
                a.Name,
                a.Icon,
                a.CreatedAt,
                a.UpdatedAt))
            .ToListAsync(cancellationToken);
    }
}
