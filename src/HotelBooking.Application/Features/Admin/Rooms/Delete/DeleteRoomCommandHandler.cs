using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Rooms.Abstractions;
using HotelBooking.Application.Features.Admin.Rooms.Common;

namespace HotelBooking.Application.Features.Admin.Rooms.Delete;

public sealed class DeleteRoomCommandHandler : ICommandHandler<DeleteRoomCommand>
{
    private readonly IRoomRepository _rooms;

    public DeleteRoomCommandHandler(IRoomRepository rooms) => _rooms = rooms;

    public async Task<Result> Handle(
        DeleteRoomCommand command,
        CancellationToken cancellationToken)
    {
        var room = await _rooms.GetByIdAsync(command.Id, command.HotelId, cancellationToken);

        if (room is null)
            return Result.Failure(RoomErrors.NotFound(command.Id));

        var hasActive = await _rooms.HasActiveBookingsAsync(command.Id, cancellationToken);

        if (hasActive)
            return Result.Failure(RoomErrors.HasActiveBookings(command.Id));

        room.IsDeleted = true;
        room.DeletedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }
}
