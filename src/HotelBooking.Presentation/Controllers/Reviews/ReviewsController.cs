using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Reviews.Common;
using HotelBooking.Application.Features.Reviews.Create;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Reviews;

[ApiController]
[Authorize]
[Route("api/v1/reviews")]
[Tags("Reviews")]
public sealed class ReviewsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public ReviewsController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpPost]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateReviewCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(command, cancellationToken);
        return result.IsSuccess
            ? result.ToCreatedResult($"/api/v1/hotels/{result.Value.HotelId}/reviews")
            : result.ToActionResult();
    }
}