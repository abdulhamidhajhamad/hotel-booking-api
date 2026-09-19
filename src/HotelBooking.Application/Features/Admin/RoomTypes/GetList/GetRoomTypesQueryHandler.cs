using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.RoomTypes.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.RoomTypes.GetList;

public sealed class GetRoomTypesQueryHandler
    : IQueryHandler<GetRoomTypesQuery, IReadOnlyList<RoomTypeDto>>
{
    private readonly IApplicationDbContext _db;

    public GetRoomTypesQueryHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<RoomTypeDto>>> Handle(
        GetRoomTypesQuery query,
        CancellationToken cancellationToken)
    {
        var items = await _db.RoomTypes
            .AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new RoomTypeDto(
                t.Id,
                t.Name,
                t.Description,
                t.Rooms.Count(r => !r.IsDeleted),
                t.CreatedAt,
                t.UpdatedAt))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<RoomTypeDto>>.Success(items);
    }
}