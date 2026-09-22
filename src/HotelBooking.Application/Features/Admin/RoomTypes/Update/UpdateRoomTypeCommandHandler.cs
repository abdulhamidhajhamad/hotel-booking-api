using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.RoomTypes.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Update;

public sealed class UpdateRoomTypeCommandHandler
    : ICommandHandler<UpdateRoomTypeCommand, RoomTypeDto>
{
    private readonly IApplicationDbContext _db;

    public UpdateRoomTypeCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<RoomTypeDto>> Handle(
        UpdateRoomTypeCommand command,
        CancellationToken cancellationToken)
    {
        var roomType = await _db.RoomTypes
            .FirstOrDefaultAsync(t => t.Id == command.Id, cancellationToken);

        if (roomType is null)
            return RoomTypeErrors.NotFound(command.Id);

        if (command.Name is not null)
        {
            var newName = command.Name.Trim();
            if (!string.Equals(newName, roomType.Name, StringComparison.OrdinalIgnoreCase))
            {
                var normalized = newName.ToLower();
                var conflictName = await _db.RoomTypes
                    .Where(t => t.Id != command.Id && t.Name.ToLower() == normalized)
                    .Select(t => t.Name)
                    .FirstOrDefaultAsync(cancellationToken);

                if (conflictName is not null)
                    return RoomTypeErrors.AlreadyExists(conflictName);
            }

            roomType.Name = newName;
        }

        if (command.Description is not null)
            roomType.Description = string.IsNullOrWhiteSpace(command.Description)
                ? null
                : command.Description.Trim();

        var numberOfRooms = await _db.Rooms
            .CountAsync(r => r.RoomTypeId == roomType.Id, cancellationToken);

        return new RoomTypeDto(
            roomType.Id,
            roomType.Name,
            roomType.Description,
            numberOfRooms,
            roomType.CreatedAt,
            roomType.UpdatedAt);
    }
}