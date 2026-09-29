using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Discounts.Abstractions;
using HotelBooking.Application.Features.Admin.Discounts.Common;

namespace HotelBooking.Application.Features.Admin.Discounts.GetById;

public sealed class GetDiscountByIdQueryHandler
    : IQueryHandler<GetDiscountByIdQuery, DiscountDto>
{
    private readonly IDiscountReader _discounts;

    public GetDiscountByIdQueryHandler(IDiscountReader discounts) => _discounts = discounts;

    public async Task<Result<DiscountDto>> Handle(
        GetDiscountByIdQuery query,
        CancellationToken cancellationToken)
    {
        var dto = await _discounts.GetByIdAsync(query.Id, cancellationToken);

        return dto is null
            ? DiscountErrors.NotFound(query.Id)
            : dto;
    }
}
