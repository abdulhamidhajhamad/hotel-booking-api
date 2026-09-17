using FluentValidation;
using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;

using HotelBooking.Application.Features.Auth.Abstractions;

namespace HotelBooking.Application.Features.Auth.Login;

public sealed class LoginCommandHandler
    : ICommandHandler<LoginCommand, LoginResponse>
{
    private readonly IUserAuthenticator _authenticator;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IRefreshTokenIssuer _refreshIssuer;
    private readonly IValidator<LoginCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public LoginCommandHandler(
        IUserAuthenticator authenticator,
        IJwtTokenGenerator tokenGenerator,
        IRefreshTokenIssuer refreshIssuer,
        IValidator<LoginCommand> validator,
        TimeProvider timeProvider)
    {
        _authenticator = authenticator;
        _tokenGenerator = tokenGenerator;
        _refreshIssuer = refreshIssuer;
        _validator = validator;
        _timeProvider = timeProvider;
    }

    public async Task<Result<LoginResponse>> Handle(
        LoginCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var message = string.Join("; ",
                validation.Errors.Select(f => $"{f.PropertyName}: {f.ErrorMessage}"));
            return Result<LoginResponse>.Failure(
                Error.Validation("Validation.Failed", message));
        }

        var authResult = await _authenticator.AuthenticateAsync(
            command.Email, command.Password, cancellationToken);
        if (authResult.IsFailure)
            return Result<LoginResponse>.Failure(authResult.Error);

        var user = authResult.Value;
        var jwt = _tokenGenerator.Generate(user.Id, user.Email, user.Roles);
        var refresh = await _refreshIssuer.IssueAsync(user.Id, jwt.Jti, cancellationToken);

        var expiresIn = (int)(jwt.ExpiresAt - _timeProvider.GetUtcNow()).TotalSeconds;
        return new LoginResponse(jwt.AccessToken, refresh.RawToken, expiresIn);
    }
}