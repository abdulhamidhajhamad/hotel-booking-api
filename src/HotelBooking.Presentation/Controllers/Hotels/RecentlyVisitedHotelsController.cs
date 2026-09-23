using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Hotels.RecentlyVisited;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Hotels;

[ApiController]
[Authorize]
[Route("api/v1/hotels")]
public sealed class RecentlyVisitedHotelsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public RecentlyVisitedHotelsController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpGet("recently-visited")]
    [ProducesResponseType(typeof(IReadOnlyList<RecentlyVisitedHotelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetRecentlyVisited(
        [FromQuery] GetRecentlyVisitedHotelsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(query, cancellationToken);
        return result.ToActionResult();
    }
}