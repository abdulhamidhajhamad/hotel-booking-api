using HotelBooking.Application.Features.Auth.Login;
using Microsoft.Extensions.Time.Testing;

namespace HotelBooking.Application.UnitTests.Features.Auth.Login;

public class LoginCommandHandlerTests
{
    private readonly IUserAuthenticator _authenticator = Substitute.For<IUserAuthenticator>();
    private readonly IJwtTokenGenerator _tokenGenerator = Substitute.For<IJwtTokenGenerator>();
    private readonly IRefreshTokenIssuer _refreshIssuer = Substitute.For<IRefreshTokenIssuer>();
    private readonly IValidator<LoginCommand> _validator = Substitute.For<IValidator<LoginCommand>>();
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
    private readonly LoginCommandHandler _sut;

    public LoginCommandHandlerTests()
    {
        _sut = new LoginCommandHandler(_authenticator, _tokenGenerator, _refreshIssuer, _validator, _time);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ReturnsValidationError()
    {
        var command = new LoginCommand("", "");
        _validator.ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new List<ValidationFailure>
            {
                new("Email", "Email is required")
            }));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_WhenAuthFails_PropagatesError()
    {
        var command = new LoginCommand("test@test.com", "wrong");
        _validator.ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _authenticator.AuthenticateAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticatedUser>.Failure(
                HotelBooking.Application.Common.Errors.AuthErrors.InvalidCredentials()));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenSuccess_ReturnsTokensAndCorrectExpiresIn()
    {
        var command = new LoginCommand("test@test.com", "P@ssw0rd1");
        var user = new AuthenticatedUser(Guid.NewGuid(), command.Email, new[] { "User" });
        var jwtExpiresAt = _time.GetUtcNow().AddMinutes(15);
        var jwt = new JwtToken("access-token", "jti-abc", jwtExpiresAt);

        _validator.ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _authenticator.AuthenticateAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns(Result<AuthenticatedUser>.Success(user));
        _tokenGenerator.Generate(user.Id, user.Email, user.Roles).Returns(jwt);
        _refreshIssuer.IssueAsync(user.Id, jwt.Jti, Arg.Any<CancellationToken>())
            .Returns(new RefreshTokenIssued("refresh-raw", _time.GetUtcNow().AddMinutes(30)));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("access-token");
        result.Value.RefreshToken.Should().Be("refresh-raw");
        result.Value.ExpiresIn.Should().Be(15 * 60);
        result.Value.TokenType.Should().Be("Bearer");
    }
}