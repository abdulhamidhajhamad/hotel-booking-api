using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Rooms.Abstractions;
using HotelBooking.Application.Features.Admin.Rooms.Common;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Admin.Rooms.Create;

public sealed class CreateRoomCommandHandler : ICommandHandler<CreateRoomCommand, RoomDetail>
{
    private readonly IRoomRepository _rooms;

    public CreateRoomCommandHandler(IRoomRepository rooms) => _rooms = rooms;

    public async Task<Result<RoomDetail>> Handle(
        CreateRoomCommand command,
        CancellationToken cancellationToken)
    {
        var number = command.Number.Trim();

        var lookup = await _rooms.GetCreateLookupAsync(
            command.HotelId, command.RoomTypeId, number, cancellationToken);

        if (lookup is null)
            return RoomErrors.HotelNotFound(command.HotelId);

        if (lookup.RoomType is null)
            return RoomErrors.RoomTypeNotFound(command.RoomTypeId);

        if (lookup.DuplicateNumber)
            return RoomErrors.DuplicateNumber(command.HotelId, number);

        var room = new Room
        {
            HotelId = command.HotelId,
            RoomTypeId = command.RoomTypeId,
            Number = number,
            AdultsCapacity = command.AdultsCapacity,
            ChildrenCapacity = command.ChildrenCapacity,
            PricePerNight = command.PricePerNight,
            IsActive = command.IsActive,
        };

        _rooms.Add(room);

        return new RoomDetail(
            room.Id,
            command.HotelId,
            lookup.HotelName,
            lookup.RoomType.Id,
            lookup.RoomType.Name,
            room.Number,
            room.AdultsCapacity,
            room.ChildrenCapacity,
            room.PricePerNight,
            room.IsActive,
            NumberOfImages: 0,
            room.CreatedAt,
            room.UpdatedAt);
    }
}
