using HotelBooking.Application.Features.Admin.HotelImages.Abstractions;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.HotelImages;

public sealed class HotelImageRepository : IHotelImageRepository
{
    private readonly ApplicationDbContext _db;

    public HotelImageRepository(ApplicationDbContext db) => _db = db;

    public Task<bool> HotelExistsAsync(Guid hotelId, CancellationToken cancellationToken) =>
        _db.Hotels.AnyAsync(h => h.Id == hotelId, cancellationToken);

    public Task<bool> HasPrimaryAsync(Guid hotelId, CancellationToken cancellationToken) =>
        _db.HotelImages.AnyAsync(i => i.HotelId == hotelId && i.IsPrimary, cancellationToken);

    public Task<HotelImage?> GetByIdAsync(Guid imageId, Guid hotelId, CancellationToken cancellationToken) =>
        _db.HotelImages.FirstOrDefaultAsync(i => i.Id == imageId && i.HotelId == hotelId, cancellationToken);

    public Task<HotelImage?> GetNextForPrimaryAsync(Guid hotelId, Guid excludeImageId, CancellationToken cancellationToken) =>
        _db.HotelImages
            .Where(i => i.HotelId == hotelId && i.Id != excludeImageId)
            .OrderBy(i => i.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<HotelImage>> GetPrimariesAsync(Guid hotelId, CancellationToken cancellationToken) =>
        await _db.HotelImages
            .Where(i => i.HotelId == hotelId && i.IsPrimary)
            .ToListAsync(cancellationToken);

    public void Add(HotelImage image) => _db.HotelImages.Add(image);

    public void Remove(HotelImage image) => _db.HotelImages.Remove(image);
}
