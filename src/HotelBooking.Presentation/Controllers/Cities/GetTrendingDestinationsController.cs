using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Cities.GetTrending;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Cities;

[ApiController]
[AllowAnonymous]
[Route("api/v1/cities")]
public sealed class GetTrendingDestinationsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public GetTrendingDestinationsController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpGet("trending")]
    [ProducesResponseType(typeof(IReadOnlyList<TrendingDestinationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTrending(
        [FromQuery] GetTrendingDestinationsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(query, cancellationToken);
        return result.ToActionResult();
    }
}