using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Hotels.Common;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Hotels.Create;

public sealed class CreateHotelCommandHandler
    : ICommandHandler<CreateHotelCommand, HotelDetail>
{
    private readonly IApplicationDbContext _db;

    public CreateHotelCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<HotelDetail>> Handle(
        CreateHotelCommand command,
        CancellationToken cancellationToken)
    {
        var city = await _db.Cities
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == command.CityId, cancellationToken);

        if (city is null)
            return HotelErrors.CityNotFound(command.CityId);

        ApplicationUser? owner = null;
        if (command.OwnerId.HasValue)
        {
            owner = await _db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == command.OwnerId.Value, cancellationToken);

            if (owner is null)
                return HotelErrors.OwnerNotFound(command.OwnerId.Value);
        }

        var hotel = new Hotel
        {
            Name = command.Name.Trim(),
            Description = string.IsNullOrWhiteSpace(command.Description) ? null : command.Description!.Trim(),
            StarRating = command.StarRating,
            Category = command.Category,
            Address = command.Address.Trim(),
            Latitude = command.Latitude,
            Longitude = command.Longitude,
            CityId = command.CityId,
            OwnerId = command.OwnerId,
        };

        await _db.Hotels.AddAsync(hotel, cancellationToken);

        return new HotelDetail(
            hotel.Id,
            hotel.Name,
            hotel.Description,
            hotel.StarRating,
            hotel.Category,
            hotel.Address,
            hotel.Latitude,
            hotel.Longitude,
            city.Id,
            city.Name,
            city.Country,
            owner?.Id,
            owner?.Email,
            owner?.FullName,
            NumberOfRooms: 0,
            PrimaryImageUrl: null,
            hotel.CreatedAt,
            hotel.UpdatedAt);
    }
}