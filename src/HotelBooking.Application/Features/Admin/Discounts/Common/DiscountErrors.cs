using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Admin.Discounts.Common;

public static class DiscountErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Discount.NotFound",
            $"Discount with id '{id}' was not found.");

    public static Error RoomNotFound(Guid roomId) =>
        Error.Validation("Discount.RoomNotFound",
            $"Room with id '{roomId}' was not found.");
}