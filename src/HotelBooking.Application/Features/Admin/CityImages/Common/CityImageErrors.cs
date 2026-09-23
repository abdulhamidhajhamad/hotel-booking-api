using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Admin.CityImages.Common;

public static class CityImageErrors
{
    public static Error CityNotFound(Guid cityId) =>
        Error.Validation("CityImage.CityNotFound",
            $"City with id '{cityId}' was not found.");

    public static Error NotFound(Guid imageId) =>
        Error.NotFound("CityImage.NotFound",
            $"City image with id '{imageId}' was not found.");
}