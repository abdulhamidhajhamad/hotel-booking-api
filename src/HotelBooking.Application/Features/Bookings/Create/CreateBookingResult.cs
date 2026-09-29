namespace HotelBooking.Application.Features.Bookings.Create;

public sealed record CreateBookingResult(
    Guid BookingGroupId,
    string ConfirmationNumber,
    decimal TotalPrice,
    DateTimeOffset HoldExpiresAt);