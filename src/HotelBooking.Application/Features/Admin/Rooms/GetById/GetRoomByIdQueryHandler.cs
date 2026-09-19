using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Rooms.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Rooms.GetById;

public sealed class GetRoomByIdQueryHandler : IQueryHandler<GetRoomByIdQuery, RoomDetail>
{
    private readonly IApplicationDbContext _db;

    public GetRoomByIdQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<RoomDetail>> Handle(
        GetRoomByIdQuery query,
        CancellationToken cancellationToken)
    {
        var room = await _db.Rooms
            .AsNoTracking()
            .Where(r => r.Id == query.Id && r.HotelId == query.HotelId)
            .Select(r => new RoomDetail(
                r.Id,
                r.HotelId,
                r.Hotel.Name,
                r.RoomTypeId,
                r.RoomType.Name,
                r.Number,
                r.AdultsCapacity,
                r.ChildrenCapacity,
                r.PricePerNight,
                r.IsActive,
                r.Images.Count(),
                r.CreatedAt,
                r.UpdatedAt))
            .FirstOrDefaultAsync(cancellationToken);

        return room is null ? RoomErrors.NotFound(query.Id) : room;
    }
}