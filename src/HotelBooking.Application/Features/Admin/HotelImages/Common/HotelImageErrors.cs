using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Admin.HotelImages.Common;

public static class HotelImageErrors
{
    public static Error HotelNotFound(Guid hotelId) =>
        Error.Validation("HotelImage.HotelNotFound",
            $"Hotel with id '{hotelId}' was not found.");

    public static Error NotFound(Guid imageId) =>
        Error.NotFound("HotelImage.NotFound",
            $"Hotel image with id '{imageId}' was not found.");
}