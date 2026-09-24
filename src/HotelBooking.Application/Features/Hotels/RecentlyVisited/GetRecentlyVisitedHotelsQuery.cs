using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Hotels.RecentlyVisited;

public sealed record GetRecentlyVisitedHotelsQuery(int Count = 5)
    : IQuery<IReadOnlyList<RecentlyVisitedHotelDto>>;