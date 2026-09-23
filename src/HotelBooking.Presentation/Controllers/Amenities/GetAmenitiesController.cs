using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Amenities.Common;
using HotelBooking.Application.Features.Admin.Amenities.GetList;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Amenities;

[ApiController]
[AllowAnonymous]
[Route("api/v1/amenities")]
public sealed class AmenitiesController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public AmenitiesController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AmenityDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetAmenitiesQuery(), cancellationToken);
        return result.ToActionResult();
    }
}