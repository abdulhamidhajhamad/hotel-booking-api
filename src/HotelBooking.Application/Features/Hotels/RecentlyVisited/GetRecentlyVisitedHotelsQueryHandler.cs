using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Hotels.RecentlyVisited.Abstractions;

namespace HotelBooking.Application.Features.Hotels.RecentlyVisited;

public sealed class GetRecentlyVisitedHotelsQueryHandler
    : IQueryHandler<GetRecentlyVisitedHotelsQuery, IReadOnlyList<RecentlyVisitedHotelDto>>
{
    private readonly IRecentlyVisitedHotelsReader _reader;
    private readonly ICurrentUser _currentUser;

    public GetRecentlyVisitedHotelsQueryHandler(IRecentlyVisitedHotelsReader reader, ICurrentUser currentUser)
    {
        _reader = reader;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<RecentlyVisitedHotelDto>>> Handle(
        GetRecentlyVisitedHotelsQuery query,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Result<IReadOnlyList<RecentlyVisitedHotelDto>>.Failure(
                Error.Unauthorized("RecentlyVisited.NotAuthenticated", "No active session."));

        var result = await _reader.GetRecentlyVisitedAsync(userId, query.Count, cancellationToken);

        return Result<IReadOnlyList<RecentlyVisitedHotelDto>>.Success(result);
    }
}
