using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Auth.Abstractions;

public interface IRefreshTokenRotator
{
    Task<Result<RotatedTokens>> RotateAsync(
        string rawRefreshToken,
        CancellationToken cancellationToken = default);
}

public sealed record RotatedTokens(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset AccessExpiresAt);