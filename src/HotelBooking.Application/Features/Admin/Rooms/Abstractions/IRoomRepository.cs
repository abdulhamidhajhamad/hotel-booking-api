using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Admin.Rooms.Abstractions;

public interface IRoomRepository
{
    Task<RoomCreateLookup?> GetCreateLookupAsync(Guid hotelId, Guid roomTypeId, string number, CancellationToken cancellationToken);

    Task<Room?> GetByIdAsync(Guid id, Guid hotelId, CancellationToken cancellationToken);

    Task<Room?> GetByIdWithRelationsAsync(Guid id, Guid hotelId, CancellationToken cancellationToken);

    Task<RoomType?> GetRoomTypeAsync(Guid roomTypeId, CancellationToken cancellationToken);

    Task<bool> HasDuplicateNumberAsync(Guid hotelId, Guid excludeRoomId, string number, CancellationToken cancellationToken);

    Task<int> CountImagesAsync(Guid roomId, CancellationToken cancellationToken);

    Task<bool> HasActiveBookingsAsync(Guid roomId, CancellationToken cancellationToken);

    void Add(Room room);
}
