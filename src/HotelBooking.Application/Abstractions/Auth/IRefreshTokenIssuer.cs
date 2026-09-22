namespace HotelBooking.Application.Features.Auth.Abstractions;

public interface IRefreshTokenIssuer
{
    Task<RefreshTokenIssued> IssueAsync(
        Guid userId,
        string jti,
        CancellationToken cancellationToken = default);
}

public sealed record RefreshTokenIssued(string RawToken, DateTimeOffset ExpiresAt);