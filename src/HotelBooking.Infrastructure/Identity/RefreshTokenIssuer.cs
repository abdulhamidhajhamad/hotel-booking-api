using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using HotelBooking.Application.Abstractions;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Identity.Options;

using HotelBooking.Application.Features.Auth.Abstractions;

namespace HotelBooking.Infrastructure.Identity;

public sealed class RefreshTokenIssuer : IRefreshTokenIssuer
{
    private readonly IApplicationDbContext _dbContext;
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenIssuer(
        IApplicationDbContext dbContext,
        IOptions<JwtOptions> options,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<RefreshTokenIssued> IssueAsync(
        Guid userId,
        string jti,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.RefreshTokenMinutes);

        var raw = GenerateRawToken();
        var hash = HashToken(raw);

        var refresh = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hash,
            Jti = jti,
            ExpiresAt = expiresAt,
            CreatedAt = now,
        };

        _dbContext.RefreshTokens.Add(refresh);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshTokenIssued(raw, expiresAt);
    }

    internal static string GenerateRawToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    internal static string HashToken(string raw)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(raw);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}