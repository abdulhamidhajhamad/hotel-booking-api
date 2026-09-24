using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Common.Results;
using HotelBooking.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Hotels.Search;

public sealed class SearchHotelsQueryHandler
    : IQueryHandler<SearchHotelsQuery, PagedResult<HotelSearchResultDto>>
{
    private readonly IApplicationDbContext _db;

    public SearchHotelsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<PagedResult<HotelSearchResultDto>>> Handle(
        SearchHotelsQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var checkIn = query.CheckIn;
        var checkOut = query.CheckOut;
        var hasDates = checkIn.HasValue && checkOut.HasValue;

        var hotels = _db.Hotels.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var term = query.Query.Trim();
            hotels = hotels.Where(h => h.Name.Contains(term) || h.City.Name.Contains(term));
        }

        if (query.MinStar.HasValue)
            hotels = hotels.Where(h => h.StarRating >= query.MinStar.Value);

        if (query.Category.HasValue)
            hotels = hotels.Where(h => h.Category == query.Category.Value);

        if (query.AmenityIds is { Count: > 0 })
        {
            foreach (var amenityId in query.AmenityIds)
                hotels = hotels.Where(h => h.HotelAmenities.Any(ha => ha.AmenityId == amenityId));
        }

        hotels = hotels.Where(h => h.Rooms.Count(r =>
            r.IsActive
            && r.AdultsCapacity >= query.Adults
            && r.ChildrenCapacity >= query.Children
            && (!hasDates || !r.Bookings.Any(b =>
                    b.Status != BookingStatus.Cancelled
                    && checkIn < b.CheckOutDate
                    && checkOut > b.CheckInDate))) >= query.Rooms);

        var projected = hotels.Select(h => new
        {
            h.Id,
            h.Name,
            CityName = h.City.Name,
            Country = h.City.Country,
            h.StarRating,
            h.Category,
            h.Description,
            ThumbnailUrl = h.Images
                .Where(i => !i.IsDeleted)
                .OrderByDescending(i => i.IsPrimary)
                .Select(i => i.Url)
                .FirstOrDefault(),
            Cheapest = h.Rooms
                .Where(r => r.IsActive
                    && r.AdultsCapacity >= query.Adults
                    && r.ChildrenCapacity >= query.Children
                    && (!hasDates || !r.Bookings.Any(b =>
                            b.Status != BookingStatus.Cancelled
                            && checkIn < b.CheckOutDate
                            && checkOut > b.CheckInDate)))
                .Select(r => new
                {
                    Original = r.PricePerNight,
                    Discounted = r.PricePerNight * (1 - (r.Discounts
                        .Where(d => d.StartUtc <= now && d.EndUtc >= now)
                        .OrderByDescending(d => d.Percentage)
                        .Select(d => d.Percentage)
                        .FirstOrDefault() / 100m))
                })
                .OrderBy(p => p.Discounted)
                .FirstOrDefault()
        });

        if (query.MinPrice.HasValue)
            projected = projected.Where(x => x.Cheapest!.Discounted >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            projected = projected.Where(x => x.Cheapest!.Discounted <= query.MaxPrice.Value);

        var totalCount = await projected.CountAsync(cancellationToken);

        projected = (query.SortBy?.ToLowerInvariant()) switch
        {
            "price"      => query.SortDesc ? projected.OrderByDescending(x => x.Cheapest!.Discounted) : projected.OrderBy(x => x.Cheapest!.Discounted),
            "starrating" => query.SortDesc ? projected.OrderByDescending(x => x.StarRating) : projected.OrderBy(x => x.StarRating),
            _            => query.SortDesc ? projected.OrderByDescending(x => x.Name) : projected.OrderBy(x => x.Name),
        };

        var items = await projected
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new HotelSearchResultDto(
                x.Id,
                x.Name,
                x.CityName,
                x.Country,
                x.StarRating,
                x.Category,
                x.Description,
                x.ThumbnailUrl,
                Math.Round(x.Cheapest!.Original, 2),
                Math.Round(x.Cheapest!.Discounted, 2)))
            .ToListAsync(cancellationToken);

        return PagedResult<HotelSearchResultDto>.Create(items, query.Page, query.PageSize, totalCount);
    }
}