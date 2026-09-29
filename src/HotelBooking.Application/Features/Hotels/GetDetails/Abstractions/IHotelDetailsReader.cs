namespace HotelBooking.Application.Features.Hotels.GetDetails.Abstractions;

public interface IHotelDetailsReader
{
    Task<HotelDetailsDto?> GetDetailsAsync(GetHotelDetailsQuery query, CancellationToken cancellationToken);
}
