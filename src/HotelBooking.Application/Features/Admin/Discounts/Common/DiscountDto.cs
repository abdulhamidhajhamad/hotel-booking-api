namespace HotelBooking.Application.Features.Admin.Discounts.Common;

public sealed record DiscountDto(
    Guid Id,
    Guid RoomId,
    string RoomNumber,
    Guid HotelId,
    string HotelName,
    string? Title,
    decimal Percentage,
    DateTime StartUtc,
    DateTime EndUtc,
    bool IsActive,
    DateTimeOffset CreatedAt);