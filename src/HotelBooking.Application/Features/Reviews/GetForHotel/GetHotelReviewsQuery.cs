using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Features.Reviews.Common;

namespace HotelBooking.Application.Features.Reviews.GetForHotel;

public sealed record GetHotelReviewsQuery(
    Guid HotelId,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ReviewDto>>;