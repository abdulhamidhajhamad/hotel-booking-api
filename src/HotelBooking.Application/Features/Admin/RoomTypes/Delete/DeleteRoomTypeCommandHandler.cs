using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.RoomTypes.Abstractions;
using HotelBooking.Application.Features.Admin.RoomTypes.Common;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Delete;

public sealed class DeleteRoomTypeCommandHandler : ICommandHandler<DeleteRoomTypeCommand>
{
    private readonly IRoomTypeRepository _roomTypes;

    public DeleteRoomTypeCommandHandler(IRoomTypeRepository roomTypes) => _roomTypes = roomTypes;

    public async Task<Result> Handle(
        DeleteRoomTypeCommand command,
        CancellationToken cancellationToken)
    {
        var roomType = await _roomTypes.GetByIdAsync(command.Id, cancellationToken);

        if (roomType is null)
            return Result.Failure(RoomTypeErrors.NotFound(command.Id));

        var inUse = await _roomTypes.HasRoomsAsync(command.Id, cancellationToken);

        if (inUse)
            return Result.Failure(RoomTypeErrors.InUseByRooms(command.Id));

        roomType.IsDeleted = true;
        roomType.DeletedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }
}
