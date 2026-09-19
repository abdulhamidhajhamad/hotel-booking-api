using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Amenities.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Amenities.GetList;

public sealed class GetAmenitiesQueryHandler
    : IQueryHandler<GetAmenitiesQuery, IReadOnlyList<AmenityDto>>
{
    private readonly IApplicationDbContext _db;

    public GetAmenitiesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<AmenityDto>>> Handle(
        GetAmenitiesQuery query,
        CancellationToken cancellationToken)
    {
        var items = await _db.Amenities
            .AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new AmenityDto(
                a.Id,
                a.Name,
                a.Icon,
                a.CreatedAt,
                a.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<AmenityDto>>.Success(items);
    }
}