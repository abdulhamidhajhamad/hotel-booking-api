namespace HotelBooking.Application.Features.Bookings.Checkout;

public sealed record CheckoutResult(
    Guid BookingGroupId,
    string ConfirmationNumber,
    string PaymentStatus,
    decimal TotalPrice);