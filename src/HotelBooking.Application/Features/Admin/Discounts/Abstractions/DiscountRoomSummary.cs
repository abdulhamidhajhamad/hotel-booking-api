namespace HotelBooking.Application.Features.Admin.Discounts.Abstractions;

public sealed record DiscountRoomSummary(Guid Id, string Number, Guid HotelId, string HotelName);
