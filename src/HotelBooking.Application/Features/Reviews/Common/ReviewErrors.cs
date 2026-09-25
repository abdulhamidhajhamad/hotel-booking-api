using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Reviews.Common;

public static class ReviewErrors
{
    public static Error NotAuthenticated() =>
        Error.Unauthorized("Review.NotAuthenticated",
            "You must be signed in to review a hotel.");

    public static Error BookingNotFound(Guid bookingId) =>
        Error.NotFound("Review.BookingNotFound",
            $"Booking with id '{bookingId}' was not found.");

    public static Error NotBookingOwner() =>
        Error.Forbidden("Review.NotBookingOwner",
            "You can only review your own bookings.");

    public static Error StayNotCompleted() =>
        Error.Conflict("Review.StayNotCompleted",
            "You can only review a hotel after a confirmed stay whose check-out date has passed.");

    public static Error AlreadyReviewed() =>
        Error.Conflict("Review.AlreadyReviewed",
            "This booking has already been reviewed.");
}