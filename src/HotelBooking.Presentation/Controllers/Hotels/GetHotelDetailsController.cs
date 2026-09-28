using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Hotels.GetDetails;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Hotels;

[ApiController]
[AllowAnonymous]
[Route("api/v1/hotels")]
public sealed class GetHotelDetailsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public GetHotelDetailsController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(HotelDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(
        Guid id,
        [FromQuery] DateOnly? checkIn = null,
        [FromQuery] DateOnly? checkOut = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _dispatcher.Send(
            new GetHotelDetailsQuery(id, checkIn, checkOut), cancellationToken);
        return result.ToActionResult();
    }
}