using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Hotels.Abstractions;
using HotelBooking.Application.Features.Admin.Hotels.Common;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Admin.Hotels.Create;

public sealed class CreateHotelCommandHandler
    : ICommandHandler<CreateHotelCommand, HotelDetail>
{
    private readonly IHotelRepository _hotels;

    public CreateHotelCommandHandler(IHotelRepository hotels) => _hotels = hotels;

    public async Task<Result<HotelDetail>> Handle(
        CreateHotelCommand command,
        CancellationToken cancellationToken)
    {
        var city = await _hotels.GetCitySummaryAsync(command.CityId, cancellationToken);

        if (city is null)
            return HotelErrors.CityNotFound(command.CityId);

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
            OwnerName = string.IsNullOrWhiteSpace(command.OwnerName) ? null : command.OwnerName!.Trim(),
        };

        _hotels.Add(hotel);

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
            NumberOfRooms: 0,
            PrimaryImageUrl: null,
            hotel.CreatedAt,
            hotel.UpdatedAt);
    }
}
