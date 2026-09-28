namespace HotelBooking.Application.Features.Bookings.Pay;

public sealed record PayBookingResult(
    Guid BookingGroupId,
    string ConfirmationNumber,
    string PaymentStatus,
    decimal TotalPrice);