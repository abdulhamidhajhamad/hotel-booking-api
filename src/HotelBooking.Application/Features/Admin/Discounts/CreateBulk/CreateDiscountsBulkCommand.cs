using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Admin.Discounts.CreateBulk;

public sealed record CreateDiscountsBulkCommand(
    IReadOnlyList<BulkDiscountItem> Items) : ICommand<BulkCreateResponse>;