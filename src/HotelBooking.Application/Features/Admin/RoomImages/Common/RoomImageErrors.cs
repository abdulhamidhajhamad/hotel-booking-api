using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Admin.RoomImages.Common;

public static class RoomImageErrors
{
    public static Error RoomNotFound(Guid hotelId, Guid roomId) =>
        Error.Validation("RoomImage.RoomNotFound",
            $"Room '{roomId}' was not found under hotel '{hotelId}'.");

    public static Error NotFound(Guid imageId) =>
        Error.NotFound("RoomImage.NotFound",
            $"Room image with id '{imageId}' was not found.");
}