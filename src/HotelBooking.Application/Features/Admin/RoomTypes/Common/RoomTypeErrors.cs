using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Common;

public static class RoomTypeErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("RoomType.NotFound",
            $"Room type with id '{id}' was not found.");

    public static Error AlreadyExists(string existingName) =>
        Error.Conflict("RoomType.AlreadyExists",
            $"A room type with the name '{existingName}' already exists.");

    public static Error InUseByRooms(Guid id) =>
        Error.Conflict("RoomType.InUseByRooms",
            $"Room type '{id}' cannot be deleted because rooms are still using it.");
}