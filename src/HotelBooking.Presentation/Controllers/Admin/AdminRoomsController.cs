using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Features.Admin.Rooms.Common;
using HotelBooking.Application.Features.Admin.Rooms.Create;
using HotelBooking.Application.Features.Admin.Rooms.Delete;
using HotelBooking.Application.Features.Admin.Rooms.GetById;
using HotelBooking.Application.Features.Admin.Rooms.GetList;
using HotelBooking.Application.Features.Admin.Rooms.Update;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/hotels/{hotelId:guid}/rooms")]
public sealed class AdminRoomsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public AdminRoomsController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<RoomGridItem>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetList(
        Guid hotelId,
        [FromQuery] GetRoomsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(query with { HotelId = hotelId }, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(RoomDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid hotelId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetRoomByIdQuery(hotelId, id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [ProducesResponseType(typeof(RoomDetail), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        Guid hotelId,
        [FromBody] CreateRoomCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(command with { HotelId = hotelId }, cancellationToken);
        return result.IsSuccess
            ? result.ToCreatedResult($"/api/v1/admin/hotels/{hotelId}/rooms/{result.Value.Id}")
            : result.ToActionResult();
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(RoomDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid hotelId,
        Guid id,
        [FromBody] UpdateRoomCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(command with { HotelId = hotelId, Id = id }, cancellationToken);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        Guid hotelId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new DeleteRoomCommand(hotelId, id), cancellationToken);
        return result.ToActionResult();
    }
}