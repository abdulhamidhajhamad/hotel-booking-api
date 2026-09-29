using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Admin.Cities.Abstractions;

public interface ICityRepository
{
    Task<City?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> ExistsWithNameAndCountryAsync(string name, string country, Guid? excludeId, CancellationToken cancellationToken);

    Task<int> CountHotelsAsync(Guid cityId, CancellationToken cancellationToken);

    Task<bool> HasHotelsAsync(Guid cityId, CancellationToken cancellationToken);

    void Add(City city);
}
