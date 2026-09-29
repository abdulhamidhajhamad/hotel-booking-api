using HotelBooking.Application.Features.Admin.Amenities.Abstractions;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.Amenities;

public sealed class AmenityRepository : IAmenityRepository
{
    private readonly ApplicationDbContext _db;

    public AmenityRepository(ApplicationDbContext db) => _db = db;

    public Task<Amenity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Amenities.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public Task<string?> GetExistingNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken)
    {
        var normalized = name.ToLower();

        return _db.Amenities
            .Where(a => a.Name.ToLower() == normalized && (excludeId == null || a.Id != excludeId))
            .Select(a => a.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public void Add(Amenity amenity) => _db.Amenities.Add(amenity);

    public void Remove(Amenity amenity) => _db.Amenities.Remove(amenity);
}
