using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Discounts.Common;

namespace HotelBooking.Application.Features.Admin.Discounts.Create;

public sealed record CreateDiscountCommand(
    Guid RoomId,
    decimal Percentage,
    DateTime StartUtc,
    DateTime EndUtc,
    string? Title) : ICommand<DiscountDto>;