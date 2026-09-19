using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Amenities.Common;
using HotelBooking.Application.Features.Admin.Amenities.Create;
using HotelBooking.Application.Features.Admin.Amenities.Delete;
using HotelBooking.Application.Features.Admin.Amenities.GetList;
using HotelBooking.Application.Features.Admin.Amenities.Update;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/amenities")]
public sealed class AdminAmenitiesController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public AdminAmenitiesController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AmenityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetAmenitiesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [ProducesResponseType(typeof(AmenityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAmenityCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(command, cancellationToken);
        return result.IsSuccess
            ? result.ToCreatedResult($"/api/v1/admin/amenities/{result.Value.Id}")
            : result.ToActionResult();
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(AmenityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateAmenityCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(command with { Id = id }, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new DeleteAmenityCommand(id), cancellationToken);
        return result.ToActionResult();
    }
}