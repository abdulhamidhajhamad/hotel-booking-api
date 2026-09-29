using HotelBooking.Application.Features.Admin.RoomTypes.Common;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Abstractions;

public interface IRoomTypeReader
{
    Task<IReadOnlyList<RoomTypeDto>> GetAllAsync(CancellationToken cancellationToken);
}
