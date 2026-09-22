using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Rooms.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Rooms.Update;

public sealed class UpdateRoomCommandHandler : ICommandHandler<UpdateRoomCommand, RoomDetail>
{
    private readonly IApplicationDbContext _db;

    public UpdateRoomCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<RoomDetail>> Handle(
        UpdateRoomCommand command,
        CancellationToken cancellationToken)
    {
        var room = await _db.Rooms
            .Include(r => r.Hotel)
            .Include(r => r.RoomType)
            .FirstOrDefaultAsync(
                r => r.Id == command.Id && r.HotelId == command.HotelId,
                cancellationToken);

        if (room is null)
            return RoomErrors.NotFound(command.Id);

        var roomType = room.RoomType;

        if (command.RoomTypeId.HasValue && command.RoomTypeId.Value != room.RoomTypeId)
        {
            var newType = await _db.RoomTypes
                .FirstOrDefaultAsync(t => t.Id == command.RoomTypeId.Value, cancellationToken);
            if (newType is null)
                return RoomErrors.RoomTypeNotFound(command.RoomTypeId.Value);

            room.RoomTypeId = newType.Id;
            room.RoomType = newType;
            roomType = newType;
        }

        if (command.Number is not null)
        {
            var newNumber = command.Number.Trim();
            if (newNumber != room.Number)
            {
                var duplicate = await _db.Rooms.AnyAsync(
                    r => r.HotelId == room.HotelId && r.Id != room.Id && r.Number == newNumber,
                    cancellationToken);
                if (duplicate)
                    return RoomErrors.DuplicateNumber(room.HotelId, newNumber);
                room.Number = newNumber;
            }
        }

        if (command.AdultsCapacity.HasValue) room.AdultsCapacity = command.AdultsCapacity.Value;
        if (command.ChildrenCapacity.HasValue) room.ChildrenCapacity = command.ChildrenCapacity.Value;
        if (command.PricePerNight.HasValue) room.PricePerNight = command.PricePerNight.Value;
        if (command.IsActive.HasValue) room.IsActive = command.IsActive.Value;

        var numberOfImages = await _db.RoomImages
            .CountAsync(i => i.RoomId == room.Id, cancellationToken);

        return new RoomDetail(
            room.Id,
            room.HotelId,
            room.Hotel.Name,
            roomType.Id,
            roomType.Name,
            room.Number,
            room.AdultsCapacity,
            room.ChildrenCapacity,
            room.PricePerNight,
            room.IsActive,
            numberOfImages,
            room.CreatedAt,
            room.UpdatedAt);
    }
}