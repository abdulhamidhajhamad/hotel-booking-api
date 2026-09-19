using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Admin.Cities.Common;

public static class CityErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("City.NotFound", $"City with id '{id}' was not found.");

    public static Error DuplicateNameCountry(string name, string country) =>
        Error.Conflict(
            "City.DuplicateNameCountry",
            $"A city with name '{name}' and country '{country}' already exists.");

    public static Error HasHotels(Guid id) =>
        Error.Conflict(
            "City.HasHotels",
            $"City '{id}' cannot be deleted because it still has active hotels.");
}