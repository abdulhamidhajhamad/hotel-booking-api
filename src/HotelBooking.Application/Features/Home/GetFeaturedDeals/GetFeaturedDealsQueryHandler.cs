using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Home.GetFeaturedDeals;

public sealed class GetFeaturedDealsQueryHandler
    : IQueryHandler<GetFeaturedDealsQuery, IReadOnlyList<FeaturedDealDto>>
{
    private readonly IApplicationDbContext _db;

    public GetFeaturedDealsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<FeaturedDealDto>>> Handle(
        GetFeaturedDealsQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var activeDeals = await _db.Discounts
            .AsNoTracking()
            .Where(d => d.StartUtc <= now && d.EndUtc >= now)
            .Where(d => d.Room.IsActive
                     && !d.Room.IsDeleted
                     && !d.Room.Hotel.IsDeleted)
            .Select(d => new
            {
                d.Room.HotelId,
                HotelName = d.Room.Hotel.Name,
                d.Room.Hotel.StarRating,
                CityName = d.Room.Hotel.City.Name,
                Country = d.Room.Hotel.City.Country,
                ThumbnailUrl = d.Room.Hotel.Images
                    .Where(i => !i.IsDeleted)
                    .OrderByDescending(i => i.IsPrimary)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                OriginalPrice = d.Room.PricePerNight,
                d.Percentage
            })
            .ToListAsync(cancellationToken);

        IReadOnlyList<FeaturedDealDto> featured = activeDeals
            .GroupBy(d => d.HotelId)
            .Select(g => g.OrderByDescending(d => d.Percentage).First())
            .OrderByDescending(d => d.Percentage)
            .Take(query.Count)
            .Select(d => new FeaturedDealDto(
                d.HotelId,
                d.HotelName,
                d.CityName,
                d.Country,
                d.StarRating,
                d.ThumbnailUrl,
                d.OriginalPrice,
                Math.Round(d.OriginalPrice * (1 - d.Percentage / 100m), 2),
                d.Percentage))
            .ToList();

        return Result<IReadOnlyList<FeaturedDealDto>>.Success(featured);
    }
}