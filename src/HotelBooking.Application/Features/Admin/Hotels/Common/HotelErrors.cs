using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Admin.Hotels.Common;

public static class HotelErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Hotel.NotFound", $"Hotel with id '{id}' was not found.");

    public static Error CityNotFound(Guid cityId) =>
        Error.Validation("Hotel.CityNotFound", $"City with id '{cityId}' was not found.");

    public static Error OwnerNotFound(Guid ownerId) =>
        Error.Validation("Hotel.OwnerNotFound", $"Owner with id '{ownerId}' was not found.");

    public static Error HasActiveBookings(Guid id) =>
        Error.Conflict("Hotel.HasActiveBookings",
            $"Hotel '{id}' cannot be deleted because it has active bookings.");
}