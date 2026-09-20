using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Features.Admin.Discounts.Common;

namespace HotelBooking.Application.Features.Admin.Discounts.GetList;

public sealed record GetDiscountsQuery(
    Guid? RoomId = null,
    Guid? HotelId = null,
    bool? ActiveOnly = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<DiscountDto>>;