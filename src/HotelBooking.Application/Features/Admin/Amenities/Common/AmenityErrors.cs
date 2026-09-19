using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Admin.Amenities.Common;

public static class AmenityErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Amenity.NotFound",
            $"Amenity with id '{id}' was not found.");

    public static Error AlreadyExists(string existingName) =>
        Error.Conflict("Amenity.AlreadyExists",
            $"An amenity with the name '{existingName}' already exists.");
}