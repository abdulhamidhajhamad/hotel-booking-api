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
        var hotel = await _db.Hotels.AsNoTracking()
            .FirstOrDefaultAsync(h => h.Id == command.HotelId, cancellationToken);
        if (hotel is null)
            return RoomErrors.HotelNotFound(command.HotelId);

        var roomType = await _db.RoomTypes.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == command.RoomTypeId, cancellationToken);
        if (roomType is null)
            return RoomErrors.RoomTypeNotFound(command.RoomTypeId);

        var number = command.Number.Trim();

        var duplicate = await _db.Rooms.AnyAsync(
            r => r.HotelId == command.HotelId && r.Number == number,
            cancellationToken);
        if (duplicate)
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
            hotel.Id,
            hotel.Name,
            roomType.Id,
            roomType.Name,
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