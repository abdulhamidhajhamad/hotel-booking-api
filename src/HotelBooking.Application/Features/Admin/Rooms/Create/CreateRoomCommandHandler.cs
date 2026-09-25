using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Rooms.Common;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Rooms.Create;

public sealed class CreateRoomCommandHandler : ICommandHandler<CreateRoomCommand, RoomDetail>
{
    private readonly IApplicationDbContext _db;

    public CreateRoomCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<RoomDetail>> Handle(
        CreateRoomCommand command,
        CancellationToken cancellationToken)
    {
        var number = command.Number.Trim();

        var lookup = await _db.Hotels
            .Where(h => h.Id == command.HotelId)
            .Select(h => new
            {
                HotelName = h.Name,
                RoomType = _db.RoomTypes
                    .Where(t => t.Id == command.RoomTypeId)
                    .Select(t => new { t.Id, t.Name })
                    .FirstOrDefault(),
                DuplicateNumber = _db.Rooms
                    .Any(r => r.HotelId == command.HotelId && r.Number == number),
            })
            .FirstOrDefaultAsync(cancellationToken);

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

        await _db.Rooms.AddAsync(room, cancellationToken);

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