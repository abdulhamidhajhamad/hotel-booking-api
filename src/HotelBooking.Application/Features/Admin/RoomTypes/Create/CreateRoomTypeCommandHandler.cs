using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.RoomTypes.Abstractions;
using HotelBooking.Application.Features.Admin.RoomTypes.Common;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Create;

public sealed class CreateRoomTypeCommandHandler
    : ICommandHandler<CreateRoomTypeCommand, RoomTypeDto>
{
    private readonly IRoomTypeRepository _roomTypes;

    public CreateRoomTypeCommandHandler(IRoomTypeRepository roomTypes) => _roomTypes = roomTypes;

    public async Task<Result<RoomTypeDto>> Handle(
        CreateRoomTypeCommand command,
        CancellationToken cancellationToken)
    {
        var name = command.Name.Trim();

        var existingName = await _roomTypes.GetExistingNameAsync(name, null, cancellationToken);

        if (existingName is not null)
            return RoomTypeErrors.AlreadyExists(existingName);

        var roomType = new RoomType
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(command.Description)
                ? null
                : command.Description.Trim(),
        };

        _roomTypes.Add(roomType);

        return new RoomTypeDto(
            roomType.Id,
            roomType.Name,
            roomType.Description,
            NumberOfRooms: 0,
            roomType.CreatedAt,
            roomType.UpdatedAt);
    }
}
