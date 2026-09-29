namespace HotelBooking.Application.Features.Bookings.Create;

public sealed record CreateBookingRoomItem(
    Guid RoomId,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Adults,
    int Children);