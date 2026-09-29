using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Cities.Abstractions;
using HotelBooking.Application.Features.Admin.Cities.Common;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Admin.Cities.Create;

public sealed class CreateCityCommandHandler
    : ICommandHandler<CreateCityCommand, CityDetail>
{
    private readonly ICityRepository _cities;

    public CreateCityCommandHandler(ICityRepository cities) => _cities = cities;

    public async Task<Result<CityDetail>> Handle(
        CreateCityCommand command,
        CancellationToken cancellationToken)
    {
        var name = command.Name.Trim();
        var country = command.Country.Trim().ToUpperInvariant();
        var postalCode = command.PostalCode?.Trim();
        var timezone = command.Timezone.Trim();

        var duplicate = await _cities.ExistsWithNameAndCountryAsync(name, country, null, cancellationToken);

        if (duplicate)
            return CityErrors.DuplicateNameCountry(name, country);

        var city = new City
        {
            Name = name,
            Country = country,
            PostalCode = postalCode,
            Timezone = timezone,
        };

        _cities.Add(city);

        return new CityDetail(
            city.Id,
            city.Name,
            city.Country,
            city.PostalCode,
            city.Timezone,
            NumberOfHotels: 0,
            city.CreatedAt,
            city.UpdatedAt);
    }
}
