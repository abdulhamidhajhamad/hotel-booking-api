using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Auth.Abstractions;

namespace HotelBooking.Application.Features.Auth.LogoutAll;

public sealed class LogoutAllCommandHandler : ICommandHandler<LogoutAllCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IJtiBlacklist _blacklist;
    private readonly IRefreshTokenRevoker _revoker;

    public LogoutAllCommandHandler(
        ICurrentUser currentUser,
        IJtiBlacklist blacklist,
        IRefreshTokenRevoker revoker)
    {
        _currentUser = currentUser;
        _blacklist = blacklist;
        _revoker = revoker;
    }

    public async Task<Result> Handle(LogoutAllCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is null)
            return Result.Failure(Error.Unauthorized("Auth.NotAuthenticated", "No active session."));

        var revoked = await _revoker.RevokeAllAsync(_currentUser.Id.Value, cancellationToken);
        foreach (var t in revoked)
            await _blacklist.AddAsync(t.Jti, t.AccessTokenExpiresAt, cancellationToken);

        return Result.Success();
    }
}