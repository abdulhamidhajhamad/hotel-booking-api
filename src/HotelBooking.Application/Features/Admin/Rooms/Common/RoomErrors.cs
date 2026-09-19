using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Admin.Rooms.Common;

public static class RoomErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Room.NotFound", $"Room with id '{id}' was not found.");

    public static Error HotelNotFound(Guid hotelId) =>
        Error.Validation("Room.HotelNotFound", $"Hotel with id '{hotelId}' was not found.");

    public static Error RoomTypeNotFound(Guid roomTypeId) =>
        Error.Validation("Room.RoomTypeNotFound", $"Room type with id '{roomTypeId}' was not found.");

    public static Error DuplicateNumber(Guid hotelId, string number) =>
        Error.Conflict("Room.DuplicateNumber",
            $"A room with number '{number}' already exists in hotel '{hotelId}'.");

    public static Error HasActiveBookings(Guid id) =>
        Error.Conflict("Room.HasActiveBookings",
            $"Room '{id}' cannot be deleted because it has active bookings.");
}