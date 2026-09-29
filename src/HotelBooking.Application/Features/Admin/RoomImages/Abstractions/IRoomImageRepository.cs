using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Admin.RoomImages.Abstractions;

public interface IRoomImageRepository
{
    Task<bool> RoomExistsAsync(Guid roomId, Guid hotelId, CancellationToken cancellationToken);

    Task<RoomImage?> GetByIdAsync(Guid imageId, Guid roomId, Guid hotelId, CancellationToken cancellationToken);

    void Add(RoomImage image);

    void Remove(RoomImage image);
}
