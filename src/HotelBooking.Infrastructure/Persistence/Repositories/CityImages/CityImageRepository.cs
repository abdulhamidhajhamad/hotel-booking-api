using HotelBooking.Application.Features.Admin.CityImages.Abstractions;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.CityImages;

public sealed class CityImageRepository : ICityImageRepository
{
    private readonly ApplicationDbContext _db;

    public CityImageRepository(ApplicationDbContext db) => _db = db;

    public Task<bool> CityExistsAsync(Guid cityId, CancellationToken cancellationToken) =>
        _db.Cities.AnyAsync(c => c.Id == cityId, cancellationToken);

    public Task<bool> HasPrimaryAsync(Guid cityId, CancellationToken cancellationToken) =>
        _db.CityImages.AnyAsync(i => i.CityId == cityId && i.IsPrimary, cancellationToken);

    public Task<CityImage?> GetByIdAsync(Guid imageId, Guid cityId, CancellationToken cancellationToken) =>
        _db.CityImages.FirstOrDefaultAsync(i => i.Id == imageId && i.CityId == cityId, cancellationToken);

    public Task<CityImage?> GetNextForPrimaryAsync(Guid cityId, Guid excludeImageId, CancellationToken cancellationToken) =>
        _db.CityImages
            .Where(i => i.CityId == cityId && i.Id != excludeImageId)
            .OrderBy(i => i.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(CityImage image) => _db.CityImages.Add(image);

    public void Remove(CityImage image) => _db.CityImages.Remove(image);
}
