using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.RoomTypes.Abstractions;
using HotelBooking.Application.Features.Admin.RoomTypes.Common;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Update;

public sealed class UpdateRoomTypeCommandHandler
    : ICommandHandler<UpdateRoomTypeCommand, RoomTypeDto>
{
    private readonly IRoomTypeRepository _roomTypes;

    public UpdateRoomTypeCommandHandler(IRoomTypeRepository roomTypes) => _roomTypes = roomTypes;

    public async Task<Result<RoomTypeDto>> Handle(
        UpdateRoomTypeCommand command,
        CancellationToken cancellationToken)
    {
        var roomType = await _roomTypes.GetByIdAsync(command.Id, cancellationToken);

        if (roomType is null)
            return RoomTypeErrors.NotFound(command.Id);

        if (command.Name is not null)
        {
            var newName = command.Name.Trim();
            if (!string.Equals(newName, roomType.Name, StringComparison.OrdinalIgnoreCase))
            {
                var conflictName = await _roomTypes.GetExistingNameAsync(newName, command.Id, cancellationToken);

                if (conflictName is not null)
                    return RoomTypeErrors.AlreadyExists(conflictName);
            }

            roomType.Name = newName;
        }

        if (command.Description is not null)
            roomType.Description = string.IsNullOrWhiteSpace(command.Description)
                ? null
                : command.Description.Trim();

        var numberOfRooms = await _roomTypes.CountRoomsAsync(roomType.Id, cancellationToken);

        return new RoomTypeDto(
            roomType.Id,
            roomType.Name,
            roomType.Description,
            numberOfRooms,
            roomType.CreatedAt,
            roomType.UpdatedAt);
    }
}
