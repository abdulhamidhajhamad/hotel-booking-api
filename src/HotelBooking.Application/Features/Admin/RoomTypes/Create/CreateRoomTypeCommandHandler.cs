using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.RoomTypes.Common;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Create;

public sealed class CreateRoomTypeCommandHandler
    : ICommandHandler<CreateRoomTypeCommand, RoomTypeDto>
{
    private readonly IApplicationDbContext _db;

    public CreateRoomTypeCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<RoomTypeDto>> Handle(
        CreateRoomTypeCommand command,
        CancellationToken cancellationToken)
    {
        var name = command.Name.Trim();
        var normalized = name.ToLower();

        var existingName = await _db.RoomTypes
            .Where(t => t.Name.ToLower() == normalized)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingName is not null)
            return RoomTypeErrors.AlreadyExists(existingName);

        var roomType = new RoomType
        {
            Name = name,
            Description = string.IsNullOrWhiteSpace(command.Description)
                ? null
                : command.Description.Trim(),
        };

        await _db.RoomTypes.AddAsync(roomType, cancellationToken);

        return new RoomTypeDto(
            roomType.Id,
            roomType.Name,
            roomType.Description,
            NumberOfRooms: 0,
            roomType.CreatedAt,
            roomType.UpdatedAt);
    }
}