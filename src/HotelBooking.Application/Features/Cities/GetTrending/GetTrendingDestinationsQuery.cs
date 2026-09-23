using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Cities.GetTrending;

public sealed record GetTrendingDestinationsQuery(int Count = 5)
    : IQuery<IReadOnlyList<TrendingDestinationDto>>;