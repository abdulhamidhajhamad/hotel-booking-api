using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Cities.Common;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Cities.Create;

public sealed class CreateCityCommandHandler
    : ICommandHandler<CreateCityCommand, CityDetail>
{
    private readonly IApplicationDbContext _db;

    public CreateCityCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<CityDetail>> Handle(
        CreateCityCommand command,
        CancellationToken cancellationToken)
    {
        var name = command.Name.Trim();
        var country = command.Country.Trim().ToUpperInvariant();
        var postalCode = command.PostalCode?.Trim();
        var timezone = command.Timezone.Trim();

        var duplicate = await _db.Cities
            .AnyAsync(c => c.Name == name && c.Country == country, cancellationToken);

        if (duplicate)
            return CityErrors.DuplicateNameCountry(name, country);

        var city = new City
        {
            Name = name,
            Country = country,
            PostalCode = postalCode,
            Timezone = timezone,
        };

        await _db.Cities.AddAsync(city, cancellationToken);

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