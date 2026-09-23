using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Home.GetFeaturedDeals;

public sealed record GetFeaturedDealsQuery(int Count = 5)
    : IQuery<IReadOnlyList<FeaturedDealDto>>;