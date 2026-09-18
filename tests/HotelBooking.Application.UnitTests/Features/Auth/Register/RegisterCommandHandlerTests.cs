using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Features.Auth.Register;

namespace HotelBooking.Application.UnitTests.Features.Auth.Register;

public class RegisterCommandHandlerTests
{
    private readonly IUserRegistrar _registrar = Substitute.For<IUserRegistrar>();
    private readonly RegisterCommandHandler _sut;

    public RegisterCommandHandlerTests()
    {
        _sut = new RegisterCommandHandler(_registrar);
    }

    [Fact]
    public async Task Handle_WhenRegistrarReturnsFailure_PropagatesTheError()
    {
        var command = new RegisterCommand("dup@test.com", "P@ssw0rd1");
        _registrar.RegisterAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure(AuthErrors.EmailAlreadyRegistered(command.Email)));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.EmailAlreadyRegistered");
        result.Error.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Handle_WhenSuccess_ReturnsRegisterResponse()
    {
        var command = new RegisterCommand("new@test.com", "P@ssw0rd1");
        var newUserId = Guid.NewGuid();
        _registrar.RegisterAsync(command.Email, command.Password, Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(newUserId));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(newUserId);
        result.Value.Email.Should().Be(command.Email);
    }
}