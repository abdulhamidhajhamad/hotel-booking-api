using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Discounts.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Discounts.GetById;

public sealed class GetDiscountByIdQueryHandler
    : IQueryHandler<GetDiscountByIdQuery, DiscountDto>
{
    private readonly IApplicationDbContext _db;

    public GetDiscountByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<DiscountDto>> Handle(
        GetDiscountByIdQuery query,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var dto = await _db.Discounts
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(d => d.Id == query.Id)
            .Select(d => new DiscountDto(
                d.Id,
                d.RoomId,
                d.Room.Number,
                d.Room.HotelId,
                d.Room.Hotel.Name,
                d.Title,
                d.Percentage,
                d.StartUtc,
                d.EndUtc,
                d.StartUtc <= now && d.EndUtc >= now,
                d.CreatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? DiscountErrors.NotFound(query.Id)
            : dto;
    }
}