using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Hotels.Search.Abstractions;

namespace HotelBooking.Application.Features.Hotels.Search;

public sealed class SearchHotelsQueryHandler
    : IQueryHandler<SearchHotelsQuery, PagedResult<HotelSearchResultDto>>
{
    private readonly IHotelSearchReader _reader;

    public SearchHotelsQueryHandler(IHotelSearchReader reader) => _reader = reader;

    public async Task<Result<PagedResult<HotelSearchResultDto>>> Handle(
        SearchHotelsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _reader.SearchAsync(query, cancellationToken);

        return result;
    }
}
