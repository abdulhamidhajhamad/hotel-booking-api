namespace HotelBooking.Application.Features.Bookings.Invoice;

public sealed record InvoiceLine(
    string HotelName,
    string HotelAddress,
    string RoomNumber,
    string RoomType,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    int Nights,
    decimal OriginalPricePerNight,
    decimal DiscountedPricePerNight,
    decimal DiscountPerNight,
    decimal LineTotal);

public sealed record InvoiceModel(
    Guid BookingGroupId,
    string ConfirmationNumber,
    string GuestName,
    string GuestEmail,
    DateTimeOffset IssuedAt,
    string PaymentStatus,
    string Currency,
    decimal TotalPrice,
    IReadOnlyList<InvoiceLine> Lines);