using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.RoomTypes.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Delete;

public sealed class DeleteRoomTypeCommandHandler : ICommandHandler<DeleteRoomTypeCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteRoomTypeCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(
        DeleteRoomTypeCommand command,
        CancellationToken cancellationToken)
    {
        var roomType = await _db.RoomTypes
            .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);

        if (roomType is null)
            return Result.Failure(RoomTypeErrors.NotFound(command.Id));

        var inUse = await _db.Rooms
            .AnyAsync(r => r.RoomTypeId == command.Id, cancellationToken);

        if (inUse)
            return Result.Failure(RoomTypeErrors.InUseByRooms(command.Id));

        roomType.IsDeleted = true;
        roomType.DeletedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }
}