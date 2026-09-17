using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Features.Auth.Logout;
using HotelBooking.Application.UnitTests.Common.Fakes;

namespace HotelBooking.Application.UnitTests.Features.Auth.Logout;

public class LogoutCommandHandlerTests
{
    private readonly IJtiBlacklist _blacklist = Substitute.For<IJtiBlacklist>();
    private readonly IRefreshTokenRevoker _revoker = Substitute.For<IRefreshTokenRevoker>();

    [Fact]
    public async Task Handle_WhenAnonymous_ReturnsUnauthorized()
    {
        ICurrentUser currentUser = FakeCurrentUser.Anonymous();
        var sut = new LogoutCommandHandler(currentUser, _blacklist, _revoker);

        var result = await sut.Handle(new LogoutCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
        await _blacklist.DidNotReceive().AddAsync(
            Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAuthenticated_BlacklistsJtiAndRevokesRefreshToken()
    {
        var userId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        ICurrentUser currentUser = FakeCurrentUser.SignedIn(userId, "jti-123", expiresAt);
        var sut = new LogoutCommandHandler(currentUser, _blacklist, _revoker);

        var result = await sut.Handle(new LogoutCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _blacklist.Received(1).AddAsync("jti-123", expiresAt, Arg.Any<CancellationToken>());
        await _revoker.Received(1).RevokeByJtiAsync(userId, "jti-123", Arg.Any<CancellationToken>());
    }
}