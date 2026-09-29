using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Bookings.Common;

public static class BookingErrors
{
    public static Error NotAuthenticated() =>
        Error.Unauthorized("Booking.NotAuthenticated", "No active session.");

    public static Error RoomNotFound(Guid roomId) =>
        Error.Validation("Booking.RoomNotFound", $"Room with id '{roomId}' was not found.");

    public static Error RoomInactive(Guid roomId) =>
        Error.Validation("Booking.RoomInactive", $"Room '{roomId}' is not available for booking.");

    public static Error RoomCapacityExceeded(Guid roomId) =>
        Error.Validation("Booking.RoomCapacityExceeded",
            $"Room '{roomId}' cannot accommodate the requested number of guests.");

    public static Error RoomNotAvailable() =>
        Error.Conflict("Booking.RoomNotAvailable",
            "One or more rooms are not available for the selected dates.");

    public static Error IdempotencyKeyReused() =>
        Error.Conflict("Booking.IdempotencyKeyReused",
            "This idempotency key was already used with a different request.");

    public static Error RequestInProgress() =>
        Error.Conflict("Booking.RequestInProgress",
            "A request with this idempotency key is still being processed.");

    public static Error PaymentFailed(string? reason) =>
        Error.Conflict("Booking.PaymentFailed", reason ?? "The payment was declined.");

    public static Error BookingGroupNotFound(Guid bookingGroupId) =>
        Error.NotFound("Booking.NotFound", $"Booking '{bookingGroupId}' was not found.");

    public static Error InvoiceForbidden() =>
        Error.Forbidden("Booking.InvoiceForbidden", "You are not allowed to access this invoice.");

    public static Error BookingForbidden() =>
        Error.Forbidden("Booking.Forbidden", "You are not allowed to pay for this booking.");

    public static Error BookingNotPending() =>
        Error.Conflict("Booking.NotPending", "This booking is no longer awaiting payment.");

    public static Error HoldExpired() =>
        Error.Conflict("Booking.HoldExpired",
            "The hold on these rooms has expired. Please start a new booking.");
}