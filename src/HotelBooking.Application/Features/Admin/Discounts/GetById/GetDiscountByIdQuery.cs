using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Discounts.Common;

namespace HotelBooking.Application.Features.Admin.Discounts.GetById;

public sealed record GetDiscountByIdQuery(Guid Id) : IQuery<DiscountDto>;