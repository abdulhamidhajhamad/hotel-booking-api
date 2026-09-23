using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Features.Hotels.Search;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Hotels;

[ApiController]
[AllowAnonymous]
[Route("api/v1/hotels")]
public sealed class SearchHotelsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public SearchHotelsController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpGet("search")]
    [ProducesResponseType(typeof(PagedResult<HotelSearchResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] SearchHotelsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(query, cancellationToken);
        return result.ToActionResult();
    }
}