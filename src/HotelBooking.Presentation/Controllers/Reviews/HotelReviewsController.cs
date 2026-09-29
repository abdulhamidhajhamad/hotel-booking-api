using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Features.Reviews.Common;
using HotelBooking.Application.Features.Reviews.GetForHotel;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Reviews;

[ApiController]
[AllowAnonymous]
[Route("api/v1/hotels/{hotelId:guid}/reviews")]
[Tags("Reviews")]
public sealed class HotelReviewsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public HotelReviewsController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetForHotel(
        Guid hotelId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _dispatcher.Send(
            new GetHotelReviewsQuery(hotelId, page, pageSize), cancellationToken);
        return result.ToActionResult();
    }
}