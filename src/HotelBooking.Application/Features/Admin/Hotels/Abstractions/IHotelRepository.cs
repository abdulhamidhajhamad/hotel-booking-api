using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Admin.Hotels.Abstractions;

public interface IHotelRepository
{
    Task<HotelCitySummary?> GetCitySummaryAsync(Guid cityId, CancellationToken cancellationToken);

    Task<City?> GetCityAsync(Guid cityId, CancellationToken cancellationToken);

    Task<Hotel?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Hotel?> GetByIdWithCityAsync(Guid id, CancellationToken cancellationToken);

    Task<int> CountRoomsAsync(Guid hotelId, CancellationToken cancellationToken);

    Task<string?> GetPrimaryImageUrlAsync(Guid hotelId, CancellationToken cancellationToken);

    Task<bool> HasActiveBookingsAsync(Guid hotelId, CancellationToken cancellationToken);

    void Add(Hotel hotel);
}
