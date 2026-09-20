namespace HotelBooking.Application.Features.Admin.Discounts.CreateBulk;

public sealed record BulkDiscountItem(
    Guid RoomId,
    decimal Percentage,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Title);