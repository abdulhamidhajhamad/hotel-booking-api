namespace HotelBooking.Application.Features.Hotels.GetFeaturedDeals.Abstractions;

public interface IFeaturedDealsReader
{
    Task<IReadOnlyList<FeaturedDealDto>> GetFeaturedDealsAsync(int count, CancellationToken cancellationToken);
}
