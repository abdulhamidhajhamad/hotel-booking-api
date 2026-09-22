using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Hotels.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Hotels.GetById;

public sealed class GetHotelByIdQueryHandler
    : IQueryHandler<GetHotelByIdQuery, HotelDetail>
{
    private readonly IApplicationDbContext _db;

    public GetHotelByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<HotelDetail>> Handle(
        GetHotelByIdQuery query,
        CancellationToken cancellationToken)
    {
        var hotel = await _db.Hotels
            .AsNoTracking()
            .Where(h => h.Id == query.Id)
            .Select(h => new HotelDetail(
                h.Id,
                h.Name,
                h.Description,
                h.StarRating,
                h.Category,
                h.Address,
                h.Latitude,
                h.Longitude,
                h.CityId,
                h.City.Name,
                h.City.Country,
                h.OwnerName,
                h.Rooms.Count(r => !r.IsDeleted),
                h.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                h.CreatedAt,
                h.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return hotel is null
            ? HotelErrors.NotFound(query.Id)
            : hotel;
    }
}