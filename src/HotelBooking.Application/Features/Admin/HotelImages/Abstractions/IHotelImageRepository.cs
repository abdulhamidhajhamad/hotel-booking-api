using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Admin.HotelImages.Abstractions;

public interface IHotelImageRepository
{
    Task<bool> HotelExistsAsync(Guid hotelId, CancellationToken cancellationToken);

    Task<bool> HasPrimaryAsync(Guid hotelId, CancellationToken cancellationToken);

    Task<HotelImage?> GetByIdAsync(Guid imageId, Guid hotelId, CancellationToken cancellationToken);

    Task<HotelImage?> GetNextForPrimaryAsync(Guid hotelId, Guid excludeImageId, CancellationToken cancellationToken);

    Task<IReadOnlyList<HotelImage>> GetPrimariesAsync(Guid hotelId, CancellationToken cancellationToken);

    void Add(HotelImage image);

    void Remove(HotelImage image);
}
