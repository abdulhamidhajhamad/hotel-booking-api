namespace HotelBooking.Application.Features.Bookings.Create.Abstractions;

public sealed record RoomPricing(
    Guid RoomId,
    bool IsActive,
    int AdultsCapacity,
    int ChildrenCapacity,
    decimal PricePerNight,
    decimal? DiscountPercentage);
