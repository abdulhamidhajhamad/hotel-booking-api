using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Hotels.RecentlyVisited;

public sealed class GetRecentlyVisitedHotelsQueryHandler
    : IQueryHandler<GetRecentlyVisitedHotelsQuery, IReadOnlyList<RecentlyVisitedHotelDto>>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public GetRecentlyVisitedHotelsQueryHandler(IApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<Result<IReadOnlyList<RecentlyVisitedHotelDto>>> Handle(
        GetRecentlyVisitedHotelsQuery query,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Result<IReadOnlyList<RecentlyVisitedHotelDto>>.Failure(
                Error.Unauthorized("RecentlyVisited.NotAuthenticated", "No active session."));

        var bookings = await _db.Bookings
            .AsNoTracking()
            .Where(b => !b.IsDeleted
                     && b.Status != BookingStatus.Cancelled
                     && b.BookingGroup.UserId == userId)
            .Select(b => new
            {
                b.Room.HotelId,
                HotelName = b.Room.Hotel.Name,
                CityName = b.Room.Hotel.City.Name,
                Country = b.Room.Hotel.City.Country,
                b.Room.Hotel.StarRating,
                ThumbnailUrl = b.Room.Hotel.Images
                    .Where(i => !i.IsDeleted)
                    .OrderByDescending(i => i.IsPrimary)
                    .Select(i => i.Url)
                    .FirstOrDefault(),
                b.Room.PricePerNight,
                b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        IReadOnlyList<RecentlyVisitedHotelDto> result = bookings
            .GroupBy(b => b.HotelId)
            .Select(g => g.OrderByDescending(b => b.CreatedAt).First())
            .OrderByDescending(b => b.CreatedAt)
            .Take(query.Count)
            .Select(b => new RecentlyVisitedHotelDto(
                b.HotelId,
                b.HotelName,
                b.CityName,
                b.Country,
                b.StarRating,
                b.ThumbnailUrl,
                Math.Round(b.PricePerNight, 2),
                b.CreatedAt))
            .ToList();

        return Result<IReadOnlyList<RecentlyVisitedHotelDto>>.Success(result);
    }
}