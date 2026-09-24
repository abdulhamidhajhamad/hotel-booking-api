namespace HotelBooking.Application.Features.Bookings.Checkout;

public sealed record CheckoutRoomItem(
    Guid RoomId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Adults,
    int Children);