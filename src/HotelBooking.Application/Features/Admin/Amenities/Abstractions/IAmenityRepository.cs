using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Admin.Amenities.Abstractions;

public interface IAmenityRepository
{
    Task<Amenity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<string?> GetExistingNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken);

    void Add(Amenity amenity);

    void Remove(Amenity amenity);
}
