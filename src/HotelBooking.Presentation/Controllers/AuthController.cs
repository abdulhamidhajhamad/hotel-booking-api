using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Auth.Login;
using HotelBooking.Application.Features.Auth.Logout;
using HotelBooking.Application.Features.Auth.LogoutAll;
using HotelBooking.Application.Features.Auth.Refresh;
using HotelBooking.Application.Features.Auth.Register;
using HotelBooking.Presentation.Common.Extensions;

namespace HotelBooking.Presentation.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly ICommandHandler<RegisterCommand, RegisterResponse> _registerHandler;
    private readonly ICommandHandler<LoginCommand, LoginResponse> _loginHandler;
    private readonly ICommandHandler<RefreshCommand, RefreshResponse> _refreshHandler;
    private readonly ICommandHandler<LogoutCommand> _logoutHandler;
    private readonly ICommandHandler<LogoutAllCommand> _logoutAllHandler;

    public AuthController(
        ICommandHandler<RegisterCommand, RegisterResponse> registerHandler,
        ICommandHandler<LoginCommand, LoginResponse> loginHandler,
        ICommandHandler<RefreshCommand, RefreshResponse> refreshHandler,
        ICommandHandler<LogoutCommand> logoutHandler,
        ICommandHandler<LogoutAllCommand> logoutAllHandler)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _refreshHandler = refreshHandler;
        _logoutHandler = logoutHandler;
        _logoutAllHandler = logoutAllHandler;
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

    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _loginHandler.Handle(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(RefreshResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _refreshHandler.Handle(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var result = await _logoutHandler.Handle(new LogoutCommand(), cancellationToken);
        return result.ToActionResult();
    }

    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var result = await _logoutAllHandler.Handle(new LogoutAllCommand(), cancellationToken);
        return result.ToActionResult();
    }
}