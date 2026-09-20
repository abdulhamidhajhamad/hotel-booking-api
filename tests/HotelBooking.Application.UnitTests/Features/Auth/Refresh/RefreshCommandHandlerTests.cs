using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Features.Auth.Refresh;
using Microsoft.Extensions.Time.Testing;

namespace HotelBooking.Application.UnitTests.Features.Auth.Refresh;

public class RefreshCommandHandlerTests
{
    private readonly IRefreshTokenRotator _rotator = Substitute.For<IRefreshTokenRotator>();
    private readonly IValidator<RefreshCommand> _validator = Substitute.For<IValidator<RefreshCommand>>();
    private readonly FakeTimeProvider _time = new(DateTimeOffset.Parse("2026-01-01T00:00:00Z"));
    private readonly RefreshCommandHandler _sut;

    public RefreshCommandHandlerTests()
    {
        _sut = new RefreshCommandHandler(_rotator, _validator, _time);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ReturnsValidationError()
    {
        var command = new RefreshCommand("");
        _validator.ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new List<ValidationFailure>
            {
                new("RefreshToken", "Required")
            }));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public async Task Handle_WhenRotatorFails_PropagatesError()
    {
        var command = new RefreshCommand("used-token");
        _validator.ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _rotator.RotateAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(Result<RotatedTokens>.Failure(AuthErrors.RefreshTokenReused()));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.RefreshTokenReused");
    }

    [Fact]
    public async Task Handle_WhenSuccess_ReturnsRotatedTokensAndCorrectExpiresIn()
    {
        var command = new RefreshCommand("valid-token");
        var accessExpiresAt = _time.GetUtcNow().AddMinutes(15);
        _validator.ValidateAsync(command, Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());
        _rotator.RotateAsync(command.RefreshToken, Arg.Any<CancellationToken>())
            .Returns(Result<RotatedTokens>.Success(
                new RotatedTokens("new-access", "new-refresh", accessExpiresAt)));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be("new-access");
        result.Value.RefreshToken.Should().Be("new-refresh");
        result.Value.ExpiresIn.Should().Be(15 * 60);
    }
}