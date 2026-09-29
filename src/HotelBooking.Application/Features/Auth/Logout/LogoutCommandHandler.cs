using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Auth.Abstractions;

namespace HotelBooking.Application.Features.Auth.Logout;

public sealed class LogoutCommandHandler : ICommandHandler<LogoutCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IJtiBlacklist _blacklist;
    private readonly IRefreshTokenRevoker _revoker;

    public LogoutCommandHandler(
        ICurrentUser currentUser,
        IJtiBlacklist blacklist,
        IRefreshTokenRevoker revoker)
    {
        _currentUser = currentUser;
        _blacklist = blacklist;
        _revoker = revoker;
    }

    public async Task<Result> Handle(LogoutCommand command, CancellationToken cancellationToken)
    {
        if (_currentUser.Id is null || _currentUser.Jti is null || _currentUser.AccessTokenExpiresAt is null)
            return Result.Failure(Error.Unauthorized("Auth.NotAuthenticated", "No active session."));

        await _blacklist.AddAsync(
            _currentUser.Jti,
            _currentUser.AccessTokenExpiresAt.Value,
            cancellationToken);

        await _revoker.RevokeByJtiAsync(
            _currentUser.Id.Value,
            _currentUser.Jti,
            cancellationToken); 

        return Result.Success();
    }
}