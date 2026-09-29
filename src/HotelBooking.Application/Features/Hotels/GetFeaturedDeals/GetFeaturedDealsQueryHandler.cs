using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Hotels.GetFeaturedDeals.Abstractions;

namespace HotelBooking.Application.Features.Hotels.GetFeaturedDeals;

public sealed class GetFeaturedDealsQueryHandler
    : IQueryHandler<GetFeaturedDealsQuery, IReadOnlyList<FeaturedDealDto>>
{
    private readonly IFeaturedDealsReader _reader;

    public GetFeaturedDealsQueryHandler(IFeaturedDealsReader reader) => _reader = reader;

    public async Task<Result<IReadOnlyList<FeaturedDealDto>>> Handle(
        GetFeaturedDealsQuery query,
        CancellationToken cancellationToken)
    {
        var featured = await _reader.GetFeaturedDealsAsync(query.Count, cancellationToken);

        return Result<IReadOnlyList<FeaturedDealDto>>.Success(featured);
    }
}
