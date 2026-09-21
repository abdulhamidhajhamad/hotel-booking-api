using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Users.Common;
using HotelBooking.Application.Features.Admin.Users.Create;
using HotelBooking.Presentation.Common.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HotelBooking.Presentation.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/admin/users")]
public sealed class AdminUsersController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public AdminUsersController(IDispatcher dispatcher) => _dispatcher = dispatcher;

    [HttpPost]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAdminUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _dispatcher.Send(command, cancellationToken);
        return result.ToActionResult();
    }
}