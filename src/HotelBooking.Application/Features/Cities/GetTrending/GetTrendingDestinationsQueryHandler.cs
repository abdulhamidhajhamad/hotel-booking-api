using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Cities.GetTrending.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace HotelBooking.Application.Features.Cities.GetTrending;

public sealed class GetTrendingDestinationsQueryHandler
    : IQueryHandler<GetTrendingDestinationsQuery, IReadOnlyList<TrendingDestinationDto>>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(1);

    private readonly ITrendingDestinationsReader _reader;
    private readonly IMemoryCache _cache;

    public GetTrendingDestinationsQueryHandler(ITrendingDestinationsReader reader, IMemoryCache cache)
    {
        _reader = reader;
        _cache = cache;
    }

    public async Task<Result<IReadOnlyList<TrendingDestinationDto>>> Handle(
        GetTrendingDestinationsQuery query,
        CancellationToken cancellationToken)
    {
        var cacheKey = $"trending-destinations-{query.Count}";

        var destinations = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheTtl;
            return await _reader.GetTrendingAsync(query.Count, cancellationToken);
        });

        return Result<IReadOnlyList<TrendingDestinationDto>>.Success(destinations!);
    }
}
