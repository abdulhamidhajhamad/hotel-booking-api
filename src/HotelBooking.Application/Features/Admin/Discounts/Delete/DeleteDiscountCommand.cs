using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Admin.Discounts.Delete;

public sealed record DeleteDiscountCommand(Guid Id) : ICommand;