using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Auth.Register;
using HotelBooking.Presentation.Common.Extensions;

namespace HotelBooking.Presentation.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ICommandHandler<RegisterCommand, RegisterResponse> _registerHandler;

    public AuthController(
        ICommandHandler<RegisterCommand, RegisterResponse> registerHandler)
    {
        _registerHandler = registerHandler;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _registerHandler.Handle(command, cancellationToken);
        return result.ToActionResult();
    }
}