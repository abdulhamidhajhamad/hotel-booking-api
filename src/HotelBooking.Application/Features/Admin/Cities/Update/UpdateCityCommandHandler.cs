using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Cities.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Cities.Update;

public sealed class UpdateCityCommandHandler
    : ICommandHandler<UpdateCityCommand, CityDetail>
{
    private readonly IApplicationDbContext _db;

    public UpdateCityCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<CityDetail>> Handle(
        UpdateCityCommand command,
        CancellationToken cancellationToken)
    {
        var city = await _db.Cities
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (city is null)
            return CityErrors.NotFound(command.Id);

        var newName = command.Name?.Trim() ?? city.Name;
        var newCountry = command.Country?.Trim().ToUpperInvariant() ?? city.Country;

        if (newName != city.Name || newCountry != city.Country)
        {
            var conflict = await _db.Cities.AnyAsync(
                c => c.Id != command.Id && c.Name == newName && c.Country == newCountry,
                cancellationToken);

            if (conflict)
                return CityErrors.DuplicateNameCountry(newName, newCountry);
        }

        city.Name = newName;
        city.Country = newCountry;

        if (command.PostalCode is not null)
            city.PostalCode = string.IsNullOrWhiteSpace(command.PostalCode)
                ? null
                : command.PostalCode.Trim();

        if (command.Timezone is not null)
            city.Timezone = command.Timezone.Trim();

        var numberOfHotels = await _db.Hotels
            .CountAsync(h => h.CityId == city.Id, cancellationToken);

        return new CityDetail(
            city.Id,
            city.Name,
            city.Country,
            city.PostalCode,
            city.Timezone,
            numberOfHotels,
            city.CreatedAt,
            city.UpdatedAt);
    }
}