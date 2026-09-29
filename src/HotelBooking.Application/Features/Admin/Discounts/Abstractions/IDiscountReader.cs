using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Features.Admin.Discounts.Common;
using HotelBooking.Application.Features.Admin.Discounts.GetList;

namespace HotelBooking.Application.Features.Admin.Discounts.Abstractions;

public interface IDiscountReader
{
    Task<DiscountDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<DiscountDto>> GetPagedAsync(GetDiscountsQuery query, CancellationToken cancellationToken);
}
