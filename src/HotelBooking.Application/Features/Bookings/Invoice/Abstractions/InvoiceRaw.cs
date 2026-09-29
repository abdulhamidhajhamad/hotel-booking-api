using HotelBooking.Domain.Common;

namespace HotelBooking.Application.Features.Bookings.Invoice.Abstractions;

public sealed record InvoiceLineRaw(
    string HotelName,
    string HotelAddress,
    string RoomNumber,
    string RoomType,
    DateOnly CheckInDate,
    DateOnly CheckOutDate,
    decimal OriginalPricePerNightSnapshot,
    decimal PricePerNightSnapshot,
    decimal TotalPrice);

public sealed record InvoiceRaw(
    Guid Id,
    string ConfirmationNumber,
    decimal TotalPrice,
    DateTimeOffset CreatedAt,
    string? GuestName,
    string? GuestEmail,
    PaymentStatus PaymentStatus,
    string? Currency,
    IReadOnlyList<InvoiceLineRaw> Lines);
