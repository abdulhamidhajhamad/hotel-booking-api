using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Bookings.Create;
using HotelBooking.Application.Features.Bookings.Pay;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Bookings;

[ApiController]
[Authorize]
[Route("api/v1/bookings")]
[Tags("Bookings")]
public sealed class BookingsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public BookingsController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpPost]
    [ProducesResponseType(typeof(CreateBookingResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBookingCommand command,
        CancellationToken cancellationToken)
    {
        if (!Request.Headers.TryGetValue("Idempotency-Key", out var keyValues)
            || string.IsNullOrWhiteSpace(keyValues.ToString()))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed",
                Detail = "The 'Idempotency-Key' header is required.",
                Type = "Booking.MissingIdempotencyKey",
            });
        }

        var result = await _dispatcher.Send(
            command with { IdempotencyKey = keyValues.ToString() },
            cancellationToken);

        return result.IsSuccess
            ? result.ToCreatedResult($"/api/v1/bookings/{result.Value.BookingGroupId}/payment")
            : result.ToActionResult();
    }

    [HttpPost("{bookingGroupId:guid}/payment")]
    [ProducesResponseType(typeof(PayBookingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Pay(
        Guid bookingGroupId,
        [FromBody] PayBookingCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(
            command with { BookingGroupId = bookingGroupId },
            cancellationToken);

        return result.ToActionResult();
    }
}