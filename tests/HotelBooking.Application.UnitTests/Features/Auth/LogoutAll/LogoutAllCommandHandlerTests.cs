using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Features.Auth.LogoutAll;
using HotelBooking.Application.UnitTests.Common.Fakes;

namespace HotelBooking.Application.UnitTests.Features.Auth.LogoutAll;

public class LogoutAllCommandHandlerTests
{
    private readonly IJtiBlacklist _blacklist = Substitute.For<IJtiBlacklist>();
    private readonly IRefreshTokenRevoker _revoker = Substitute.For<IRefreshTokenRevoker>();

    [Fact]
    public async Task Handle_WhenAnonymous_ReturnsUnauthorized()
    {
        ICurrentUser currentUser = FakeCurrentUser.Anonymous();
        var sut = new LogoutAllCommandHandler(currentUser, _blacklist, _revoker);

        var result = await sut.Handle(new LogoutAllCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenAuthenticated_RevokesAllTokensAndBlacklistsEach()
    {
        var userId = Guid.NewGuid();
        var t1 = new RevokedRefreshToken("jti-1", DateTimeOffset.UtcNow.AddMinutes(10));
        var t2 = new RevokedRefreshToken("jti-2", DateTimeOffset.UtcNow.AddMinutes(20));
        _revoker.RevokeAllAsync(userId, Arg.Any<CancellationToken>())
            .Returns(new[] { t1, t2 });

        ICurrentUser currentUser = FakeCurrentUser.SignedIn(userId);
        var sut = new LogoutAllCommandHandler(currentUser, _blacklist, _revoker);

        var result = await sut.Handle(new LogoutAllCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _revoker.Received(1).RevokeAllAsync(userId, Arg.Any<CancellationToken>());
        await _blacklist.Received(1).AddAsync("jti-1", t1.AccessTokenExpiresAt, Arg.Any<CancellationToken>());
        await _blacklist.Received(1).AddAsync("jti-2", t2.AccessTokenExpiresAt, Arg.Any<CancellationToken>());
    }
}