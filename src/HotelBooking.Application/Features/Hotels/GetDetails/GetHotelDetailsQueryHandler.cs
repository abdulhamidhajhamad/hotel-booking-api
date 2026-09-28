using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Hotels.GetDetails;

public sealed class GetHotelDetailsQueryHandler
    : IQueryHandler<GetHotelDetailsQuery, HotelDetailsDto>
{
    private readonly IApplicationDbContext _db;

    public GetHotelDetailsQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<HotelDetailsDto>> Handle(
        GetHotelDetailsQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var checkIn = query.CheckIn;
        var checkOut = query.CheckOut;
        var hasDates = checkIn.HasValue && checkOut.HasValue;

        var hotel = await _db.Hotels
            .AsNoTracking()
            .Where(h => h.Id == query.HotelId)
            .Select(h => new HotelDetailsDto(
                h.Id,
                h.Name,
                h.Description,
                h.StarRating,
                h.Category,
                h.Address,
                h.Latitude,
                h.Longitude,
                h.City.Name,
                h.City.Country,
                h.OwnerName,
                h.Images
                    .Where(i => !i.IsDeleted)
                    .OrderByDescending(i => i.IsPrimary)
                    .Select(i => new HotelImageDto(i.Id, i.Url, i.IsPrimary))
                    .ToList(),
                h.HotelAmenities
                    .Select(ha => new HotelAmenityDto(
                        ha.Amenity.Id, ha.Amenity.Name, ha.Amenity.Icon))
                    .ToList(),
                h.Rooms
                    .Where(r => r.IsActive
                        && (!hasDates || !r.Bookings.Any(b =>
                                b.Status != BookingStatus.Cancelled
                                && checkIn < b.CheckOutDate
                                && checkOut > b.CheckInDate)))
                    .Select(r => new HotelRoomDto(
                        r.Id,
                        r.Number,
                        r.RoomType.Name,
                        r.RoomType.Description,
                        r.AdultsCapacity,
                        r.ChildrenCapacity,
                        Math.Round(r.PricePerNight, 2),
                        Math.Round(r.PricePerNight * (1 - (r.Discounts
                            .Where(d => d.StartUtc <= now && d.EndUtc >= now)
                            .OrderByDescending(d => d.Percentage)
                            .Select(d => d.Percentage)
                            .FirstOrDefault() / 100m)), 2),
                        r.Images
                            .Where(i => !i.IsDeleted)
                            .Select(i => i.Url)
                            .FirstOrDefault()))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);

        return hotel is null
            ? Error.NotFound("Hotel.NotFound", $"Hotel {query.HotelId} was not found.")
            : hotel;
    }
}