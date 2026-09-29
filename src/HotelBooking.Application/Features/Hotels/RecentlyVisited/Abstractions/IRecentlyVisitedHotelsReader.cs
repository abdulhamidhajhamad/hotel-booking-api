namespace HotelBooking.Application.Features.Hotels.RecentlyVisited.Abstractions;

public interface IRecentlyVisitedHotelsReader
{
    Task<IReadOnlyList<RecentlyVisitedHotelDto>> GetRecentlyVisitedAsync(Guid userId, int count, CancellationToken cancellationToken);
}
