using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Outbox.GetDeadLetters;
using HotelBooking.Application.Features.Admin.Outbox.Requeue;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/outbox")]
[Tags("Admin - Outbox")]
public sealed class AdminOutboxController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public AdminOutboxController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpGet("dead-letters")]
    [ProducesResponseType(typeof(IReadOnlyList<OutboxDeadLetterDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetDeadLetters(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _dispatcher.Send(new GetDeadLettersQuery(skip, take), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/requeue")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Requeue(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new RequeueOutboxCommand(id), cancellationToken);
        return result.ToActionResult();
    }
}
