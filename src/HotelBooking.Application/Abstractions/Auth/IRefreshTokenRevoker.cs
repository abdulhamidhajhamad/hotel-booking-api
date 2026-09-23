namespace HotelBooking.Application.Features.Auth.Abstractions;

public interface IRefreshTokenRevoker
{
    Task RevokeByJtiAsync(
        Guid userId,
        string jti,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RevokedRefreshToken>> RevokeAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record RevokedRefreshToken(string Jti, DateTimeOffset AccessTokenExpiresAt);