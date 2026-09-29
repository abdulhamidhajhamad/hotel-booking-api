using HotelBooking.Application.Features.Admin.Cities.Abstractions;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories;

public sealed class CityRepository : ICityRepository
{
    private readonly ApplicationDbContext _db;

    public CityRepository(ApplicationDbContext db) => _db = db;

    public Task<City?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Cities.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<bool> ExistsWithNameAndCountryAsync(string name, string country, Guid? excludeId, CancellationToken cancellationToken) =>
        _db.Cities.AnyAsync(
            c => c.Name == name && c.Country == country && (excludeId == null || c.Id != excludeId),
            cancellationToken);

    public Task<int> CountHotelsAsync(Guid cityId, CancellationToken cancellationToken) =>
        _db.Hotels.CountAsync(h => h.CityId == cityId, cancellationToken);

    public Task<bool> HasHotelsAsync(Guid cityId, CancellationToken cancellationToken) =>
        _db.Hotels.AnyAsync(h => h.CityId == cityId, cancellationToken);

    public void Add(City city) => _db.Cities.Add(city);
}
