using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Domain.Common;

namespace HotelBooking.Application.Features.Hotels.Search;

public sealed record SearchHotelsQuery(
    string? Query = null,
    DateOnly? CheckIn = null,
    DateOnly? CheckOut = null,
    int Adults = 2,
    int Children = 0,
    int Rooms = 1,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    int? MinStar = null,
    HotelCategory? Category = null,
    List<Guid>? AmenityIds = null,
    string? SortBy = null,
    bool SortDesc = false,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<HotelSearchResultDto>>;