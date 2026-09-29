using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Admin.CityImages.Abstractions;

public interface ICityImageRepository
{
    Task<bool> CityExistsAsync(Guid cityId, CancellationToken cancellationToken);

    Task<bool> HasPrimaryAsync(Guid cityId, CancellationToken cancellationToken);

    Task<CityImage?> GetByIdAsync(Guid imageId, Guid cityId, CancellationToken cancellationToken);

    Task<CityImage?> GetNextForPrimaryAsync(Guid cityId, Guid excludeImageId, CancellationToken cancellationToken);

    void Add(CityImage image);

    void Remove(CityImage image);
}
