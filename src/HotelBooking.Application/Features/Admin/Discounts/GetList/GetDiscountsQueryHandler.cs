using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Discounts.Abstractions;
using HotelBooking.Application.Features.Admin.Discounts.Common;

namespace HotelBooking.Application.Features.Admin.Discounts.GetList;

public sealed class GetDiscountsQueryHandler
    : IQueryHandler<GetDiscountsQuery, PagedResult<DiscountDto>>
{
    private readonly IDiscountReader _discounts;

    public GetDiscountsQueryHandler(IDiscountReader discounts) => _discounts = discounts;

    public async Task<Result<PagedResult<DiscountDto>>> Handle(
        GetDiscountsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _discounts.GetPagedAsync(query, cancellationToken);

        return result;
    }
}
