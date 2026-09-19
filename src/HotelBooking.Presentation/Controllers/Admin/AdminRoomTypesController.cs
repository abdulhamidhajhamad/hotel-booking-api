using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.RoomTypes.Common;
using HotelBooking.Application.Features.Admin.RoomTypes.Create;
using HotelBooking.Application.Features.Admin.RoomTypes.Delete;
using HotelBooking.Application.Features.Admin.RoomTypes.GetList;
using HotelBooking.Application.Features.Admin.RoomTypes.Update;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/room-types")]
public sealed class AdminRoomTypesController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public AdminRoomTypesController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RoomTypeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetList(CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetRoomTypesQuery(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [ProducesResponseType(typeof(RoomTypeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRoomTypeCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(command, cancellationToken);
        return result.IsSuccess
            ? result.ToCreatedResult($"/api/v1/admin/room-types/{result.Value.Id}")
            : result.ToActionResult();
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(RoomTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateRoomTypeCommand command,
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
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new DeleteRoomTypeCommand(id), cancellationToken);
        return result.ToActionResult();
    }
}