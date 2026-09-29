namespace HotelBooking.Application.Features.Bookings.Create.Abstractions;

public sealed record BookingGroupSnapshot(
    Guid Id,
    string ConfirmationNumber,
    decimal TotalPrice,
    DateTimeOffset CreatedAt);
