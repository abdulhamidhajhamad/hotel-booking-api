using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Hotels.Abstractions;
using HotelBooking.Application.Features.Admin.Hotels.Common;

namespace HotelBooking.Application.Features.Admin.Hotels.GetList;

public sealed class GetHotelsQueryHandler
    : IQueryHandler<GetHotelsQuery, PagedResult<HotelGridItem>>
{
    private readonly IHotelReader _hotels;

    public GetHotelsQueryHandler(IHotelReader hotels) => _hotels = hotels;

    public async Task<Result<PagedResult<HotelGridItem>>> Handle(
        GetHotelsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _hotels.GetPagedAsync(query, cancellationToken);

        return result;
    }
}
