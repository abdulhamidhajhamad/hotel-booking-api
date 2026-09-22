using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Hotels.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Hotels.Update;

public sealed class UpdateHotelCommandHandler
    : ICommandHandler<UpdateHotelCommand, HotelDetail>
{
    private readonly IApplicationDbContext _db;

    public UpdateHotelCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<HotelDetail>> Handle(
        UpdateHotelCommand command,
        CancellationToken cancellationToken)
    {
        var hotel = await _db.Hotels
            .Include(h => h.City)
            .FirstOrDefaultAsync(h => h.Id == command.Id, cancellationToken);

        if (hotel is null)
            return HotelErrors.NotFound(command.Id);

        var city = hotel.City;

        if (command.CityId.HasValue && command.CityId.Value != hotel.CityId)
        {
            var newCity = await _db.Cities
                .FirstOrDefaultAsync(c => c.Id == command.CityId.Value, cancellationToken);

            if (newCity is null)
                return HotelErrors.CityNotFound(command.CityId.Value);

            hotel.CityId = newCity.Id;
            hotel.City = newCity;
            city = newCity;
        }

        if (command.Name is not null)
            hotel.Name = command.Name.Trim();

        if (command.Description is not null)
            hotel.Description = string.IsNullOrWhiteSpace(command.Description)
                ? null
                : command.Description.Trim();

        if (command.StarRating.HasValue) hotel.StarRating = command.StarRating.Value;
        if (command.Category.HasValue) hotel.Category = command.Category.Value;
        if (command.Address is not null) hotel.Address = command.Address.Trim();
        if (command.Latitude.HasValue) hotel.Latitude = command.Latitude.Value;
        if (command.Longitude.HasValue) hotel.Longitude = command.Longitude.Value;

        if (command.OwnerName is not null)
            hotel.OwnerName = string.IsNullOrWhiteSpace(command.OwnerName)
                ? null
                : command.OwnerName.Trim();

        var numberOfRooms = await _db.Rooms
            .CountAsync(r => r.HotelId == hotel.Id, cancellationToken);

        var primaryImageUrl = await _db.HotelImages
            .Where(i => i.HotelId == hotel.Id && i.IsPrimary)
            .Select(i => i.Url)
            .FirstOrDefaultAsync(cancellationToken);

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
            hotel.OwnerName,
            numberOfRooms,
            primaryImageUrl,
            hotel.CreatedAt,
            hotel.UpdatedAt);
    }
}