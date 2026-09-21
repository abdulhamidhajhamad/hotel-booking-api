using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Auth.Register;

namespace HotelBooking.Application.UnitTests.Common.Messaging;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WhenNoValidators_CallsNext()
    {
        var behavior = new ValidationBehavior<RegisterCommand, Result<RegisterResponse>>(
            Array.Empty<IValidator<RegisterCommand>>());
        var command = new RegisterCommand("a@b.com", "alice", "P@ssw0rd1");
        var expected = Result<RegisterResponse>.Success(
            new RegisterResponse(Guid.NewGuid(), command.Email, command.UserName));

        var result = await behavior.Handle(command, () => Task.FromResult(expected), CancellationToken.None);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Handle_WhenValidationSucceeds_CallsNext()
    {
        var validator = Substitute.For<IValidator<RegisterCommand>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<RegisterCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<RegisterCommand, Result<RegisterResponse>>(new[] { validator });
        var command = new RegisterCommand("a@b.com", "alice", "P@ssw0rd1");
        var expected = Result<RegisterResponse>.Success(
            new RegisterResponse(Guid.NewGuid(), command.Email, command.UserName));

        var result = await behavior.Handle(command, () => Task.FromResult(expected), CancellationToken.None);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task Handle_WhenValidationFails_ReturnsValidationErrorAndSkipsNext()
    {
        var validator = Substitute.For<IValidator<RegisterCommand>>();
        validator.ValidateAsync(Arg.Any<ValidationContext<RegisterCommand>>(), Arg.Any<CancellationToken>())
            .Returns(new ValidationResult(new List<ValidationFailure>
            {
                new("Email", "Invalid email")
            }));

        var behavior = new ValidationBehavior<RegisterCommand, Result<RegisterResponse>>(new[] { validator });
        var command = new RegisterCommand("bad", "al", "short");
        var nextCalled = false;

        var result = await behavior.Handle(
            command,
            () => { nextCalled = true; return Task.FromResult(Result<RegisterResponse>.Success(
                new RegisterResponse(Guid.NewGuid(), command.Email, command.UserName))); },
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        nextCalled.Should().BeFalse();
    }
}