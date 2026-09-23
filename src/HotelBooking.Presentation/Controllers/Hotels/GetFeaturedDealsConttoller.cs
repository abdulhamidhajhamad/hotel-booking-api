using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Hotels.GetFeaturedDeals;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Hotels;

[ApiController]
[AllowAnonymous]
[Route("api/v1/hotels")]
public sealed class GetFeaturedDealsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public GetFeaturedDealsController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpGet("featured-deals")]
    [ProducesResponseType(typeof(IReadOnlyList<FeaturedDealDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetFeaturedDeals(
        [FromQuery] GetFeaturedDealsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(query, cancellationToken);
        return result.ToActionResult();
    }
}