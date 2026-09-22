using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Features.Admin.Rooms.Common;

namespace HotelBooking.Application.Features.Admin.Rooms.GetList;

public sealed record GetRoomsQuery(
    Guid HotelId,
    Guid? RoomTypeId = null,
    int? MinAdults = null,
    int? MinChildren = null,
    bool? IsActive = null,
    string? Search = null,
    string? SortBy = null,
    bool SortDesc = false,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<RoomGridItem>>;