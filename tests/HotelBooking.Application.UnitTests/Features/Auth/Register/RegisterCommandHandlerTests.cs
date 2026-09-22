using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Features.Auth.Common;
using HotelBooking.Application.Features.Auth.Register;

namespace HotelBooking.Application.UnitTests.Features.Auth.Register;

public class RegisterCommandHandlerTests
{
    private readonly IUserRegistrar _registrar = Substitute.For<IUserRegistrar>();
    private readonly IEmailConfirmationTokenIssuer _tokenIssuer = Substitute.For<IEmailConfirmationTokenIssuer>();
    private readonly IOutbox _outbox = Substitute.For<IOutbox>();
    private readonly RegisterCommandHandler _sut;

    public RegisterCommandHandlerTests()
    {
        _sut = new RegisterCommandHandler(_registrar, _tokenIssuer, _outbox);
    }

    [Fact]
    public async Task Handle_WhenRegistrarReturnsFailure_PropagatesTheError()
    {
        var command = new RegisterCommand("dup@test.com", "dupuser", "P@ssw0rd1");
        _registrar.RegisterAsync(command.Email, command.UserName, command.Password, Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Failure(AuthErrors.EmailAlreadyRegistered(command.Email)));

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Auth.EmailAlreadyRegistered");
        result.Error.Type.Should().Be(ErrorType.Conflict);

        await _tokenIssuer.DidNotReceive().IssueAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        await _outbox.DidNotReceive().EnqueueAsync(Arg.Any<UserRegisteredEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSuccess_IssuesTokenAndEnqueuesEvent()
    {
        var command = new RegisterCommand("new@test.com", "newuser", "P@ssw0rd1");
        var newUserId = Guid.NewGuid();
        var rawToken = "raw-token-value";

        _registrar.RegisterAsync(command.Email, command.UserName, command.Password, Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(newUserId));
        _tokenIssuer.IssueAsync(newUserId, Arg.Any<CancellationToken>())
            .Returns(rawToken);

        var result = await _sut.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(newUserId);
        result.Value.Email.Should().Be(command.Email);
        result.Value.UserName.Should().Be(command.UserName);

        await _tokenIssuer.Received(1).IssueAsync(newUserId, Arg.Any<CancellationToken>());
        await _outbox.Received(1).EnqueueAsync(
            Arg.Is<UserRegisteredEvent>(e =>
                e.UserId == newUserId
                && e.Email == command.Email
                && e.UserName == command.UserName
                && e.ConfirmationToken == rawToken),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenSuccess_CallsCollaboratorsInOrder()
    {
        var command = new RegisterCommand("order@test.com", "orderuser", "P@ssw0rd1");
        var newUserId = Guid.NewGuid();

        _registrar.RegisterAsync(command.Email, command.UserName, command.Password, Arg.Any<CancellationToken>())
            .Returns(Result<Guid>.Success(newUserId));
        _tokenIssuer.IssueAsync(newUserId, Arg.Any<CancellationToken>())
            .Returns("token");

        await _sut.Handle(command, CancellationToken.None);

        Received.InOrder(async () =>
        {
            await _registrar.RegisterAsync(command.Email, command.UserName, command.Password, Arg.Any<CancellationToken>());
            await _tokenIssuer.IssueAsync(newUserId, Arg.Any<CancellationToken>());
            await _outbox.EnqueueAsync(Arg.Any<UserRegisteredEvent>(), Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_ForwardsCancellationTokenToAllCollaborators()
    {
        var command = new RegisterCommand("ct@test.com", "ctuser", "P@ssw0rd1");
        var newUserId = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        var ct = cts.Token;

        _registrar.RegisterAsync(command.Email, command.UserName, command.Password, ct)
            .Returns(Result<Guid>.Success(newUserId));
        _tokenIssuer.IssueAsync(newUserId, ct)
            .Returns("token");

        await _sut.Handle(command, ct);

        await _registrar.Received(1).RegisterAsync(command.Email, command.UserName, command.Password, ct);
        await _tokenIssuer.Received(1).IssueAsync(newUserId, ct);
        await _outbox.Received(1).EnqueueAsync(Arg.Any<UserRegisteredEvent>(), ct);
    }
}