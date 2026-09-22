using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Features.Admin.Cities.Common;

namespace HotelBooking.Application.Features.Admin.Cities.GetList;

public sealed record GetCitiesQuery(
    string? Search = null,
    string? SortBy = null,
    bool SortDesc = false,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<CityGridItem>>;