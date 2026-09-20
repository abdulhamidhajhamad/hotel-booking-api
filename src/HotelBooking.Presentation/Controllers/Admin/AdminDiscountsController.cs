using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Pagination;
using HotelBooking.Application.Features.Admin.Discounts.Common;
using HotelBooking.Application.Features.Admin.Discounts.Create;
using HotelBooking.Application.Features.Admin.Discounts.CreateBulk;
using HotelBooking.Application.Features.Admin.Discounts.Delete;
using HotelBooking.Application.Features.Admin.Discounts.GetById;
using HotelBooking.Application.Features.Admin.Discounts.GetList;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers.Admin;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/discounts")]
public sealed class AdminDiscountsController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public AdminDiscountsController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DiscountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetList(
        [FromQuery] GetDiscountsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(query, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DiscountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new GetDiscountByIdQuery(id), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost]
    [ProducesResponseType(typeof(DiscountDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] CreateDiscountCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(command, cancellationToken);
        return result.IsSuccess
            ? result.ToCreatedResult($"/api/v1/admin/discounts/{result.Value.Id}")
            : result.ToActionResult();
    }

    [HttpPost("bulk")]
    [ProducesResponseType(typeof(BulkCreateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(BulkCreateResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateBulk(
        [FromBody] CreateDiscountsBulkCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(command, cancellationToken);

        if (!result.IsSuccess)
            return result.ToActionResult();

        var body = result.Value;
        if (body.Errors.Count > 0)
            return new BadRequestObjectResult(body);

        return new CreatedResult("/api/v1/admin/discounts", body);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(new DeleteDiscountCommand(id), cancellationToken);
        return result.ToActionResult();
    }
}