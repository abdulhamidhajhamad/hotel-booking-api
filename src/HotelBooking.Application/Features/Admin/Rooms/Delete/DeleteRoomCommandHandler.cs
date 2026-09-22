using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Rooms.Common;
using HotelBooking.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Rooms.Delete;

public sealed class DeleteRoomCommandHandler : ICommandHandler<DeleteRoomCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteRoomCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(
        DeleteRoomCommand command,
        CancellationToken cancellationToken)
    {
        var room = await _db.Rooms.FirstOrDefaultAsync(
            r => r.Id == command.Id && r.HotelId == command.HotelId,
            cancellationToken);

        if (room is null)
            return Result.Failure(RoomErrors.NotFound(command.Id));

        var hasActive = await _db.Bookings.AnyAsync(
            b => b.RoomId == command.Id &&
                 (b.Status == BookingStatus.Pending ||
                  b.Status == BookingStatus.Confirmed ||
                  b.Status == BookingStatus.CheckedIn),
            cancellationToken);

        if (hasActive)
            return Result.Failure(RoomErrors.HasActiveBookings(command.Id));

        room.IsDeleted = true;
        room.DeletedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }
}