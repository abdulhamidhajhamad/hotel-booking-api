using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Features.Reviews.Common;
using HotelBooking.Application.Features.Reviews.GetForHotel;

namespace HotelBooking.Application.Features.Reviews.Abstractions;

public interface IReviewReader
{
    Task<PagedResult<ReviewDto>> GetForHotelAsync(GetHotelReviewsQuery query, CancellationToken cancellationToken);
}
