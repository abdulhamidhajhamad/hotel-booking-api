namespace HotelBooking.Application.Features.Cities.GetTrending.Abstractions;

public interface ITrendingDestinationsReader
{
    Task<IReadOnlyList<TrendingDestinationDto>> GetTrendingAsync(int count, CancellationToken cancellationToken);
}
