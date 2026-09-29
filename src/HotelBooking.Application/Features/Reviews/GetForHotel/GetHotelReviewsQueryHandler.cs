using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Reviews.Abstractions;
using HotelBooking.Application.Features.Reviews.Common;

namespace HotelBooking.Application.Features.Reviews.GetForHotel;

public sealed class GetHotelReviewsQueryHandler
    : IQueryHandler<GetHotelReviewsQuery, PagedResult<ReviewDto>>
{
    private readonly IReviewReader _reviews;

    public GetHotelReviewsQueryHandler(IReviewReader reviews) => _reviews = reviews;

    public async Task<Result<PagedResult<ReviewDto>>> Handle(
        GetHotelReviewsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _reviews.GetForHotelAsync(query, cancellationToken);

        return result;
    }
}
