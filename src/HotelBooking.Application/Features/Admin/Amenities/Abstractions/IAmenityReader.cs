using HotelBooking.Application.Features.Admin.Amenities.Common;

namespace HotelBooking.Application.Features.Admin.Amenities.Abstractions;

public interface IAmenityReader
{
    Task<IReadOnlyList<AmenityDto>> GetAllAsync(CancellationToken cancellationToken);
}
