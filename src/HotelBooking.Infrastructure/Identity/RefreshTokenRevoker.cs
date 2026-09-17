using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using HotelBooking.Application.Abstractions;
using HotelBooking.Infrastructure.Identity.Options;

using HotelBooking.Application.Features.Auth.Abstractions;

namespace HotelBooking.Infrastructure.Identity;

public sealed class RefreshTokenRevoker : IRefreshTokenRevoker
{
    private readonly IApplicationDbContext _dbContext;
    private readonly JwtOptions _options;
    private readonly TimeProvider _timeProvider;

    public RefreshTokenRevoker(
        IApplicationDbContext dbContext,
        IOptions<JwtOptions> options,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task RevokeByJtiAsync(
        Guid userId,
        string jti,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var token = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.UserId == userId && t.Jti == jti && !t.IsRevoked, cancellationToken);
        if (token is null) return;

        token.IsRevoked = true;
        token.RevokedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RevokedRefreshToken>> RevokeAllAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var active = await _dbContext.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked && t.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        var result = new List<RevokedRefreshToken>(active.Count);
        foreach (var t in active)
        {
            t.IsRevoked = true;
            t.RevokedAt = now;
            var accessExp = t.CreatedAt.AddMinutes(_options.AccessTokenMinutes);
            if (accessExp > now)
                result.Add(new RevokedRefreshToken(t.Jti, accessExp));
        }
        await _dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }
}