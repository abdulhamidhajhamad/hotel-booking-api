using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Features.Admin.Hotels.Common;
using HotelBooking.Domain.Common;

namespace HotelBooking.Application.Features.Admin.Hotels.GetList;

public sealed record GetHotelsQuery(
    string? Search = null,
    Guid? CityId = null,
    int? MinStar = null,
    HotelCategory? Category = null,
    Guid? OwnerId = null,
    string? SortBy = null,
    bool SortDesc = false,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<HotelGridItem>>;