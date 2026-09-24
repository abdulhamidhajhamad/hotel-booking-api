using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Bookings.Checkout;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Bookings;

[ApiController]
[Authorize]
[Route("api/v1/bookings")]
public sealed class CheckoutController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public CheckoutController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpPost("checkout")]
    [ProducesResponseType(typeof(CheckoutResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Checkout(
        [FromBody] CheckoutCommand command,
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
            ? result.ToCreatedResult($"/api/v1/bookings/{result.Value.BookingGroupId}/invoice")
            : result.ToActionResult();
    }
}