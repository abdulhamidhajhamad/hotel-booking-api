using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Abstractions;

public interface IRoomTypeRepository
{
    Task<RoomType?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<string?> GetExistingNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken);

    Task<int> CountRoomsAsync(Guid roomTypeId, CancellationToken cancellationToken);

    Task<bool> HasRoomsAsync(Guid roomTypeId, CancellationToken cancellationToken);

    void Add(RoomType roomType);
}
