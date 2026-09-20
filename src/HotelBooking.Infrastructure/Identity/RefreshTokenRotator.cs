using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Auth.Abstractions;
using HotelBooking.Domain.Identity;

namespace HotelBooking.Infrastructure.Identity;

public sealed class RefreshTokenRotator : IRefreshTokenRotator
{
    private readonly IApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IRefreshTokenIssuer _issuer;
    private readonly TimeProvider _timeProvider;
    private readonly IServiceScopeFactory _scopeFactory;

    public RefreshTokenRotator(
        IApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        IJwtTokenGenerator tokenGenerator,
        IRefreshTokenIssuer issuer,
        TimeProvider timeProvider,
        IServiceScopeFactory scopeFactory)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
        _issuer = issuer;
        _timeProvider = timeProvider;
        _scopeFactory = scopeFactory;
    }

    public async Task<Result<RotatedTokens>> RotateAsync(
        string rawRefreshToken,
        CancellationToken cancellationToken = default)
    {
        var hash = RefreshTokenIssuer.HashToken(rawRefreshToken);
        var now = _timeProvider.GetUtcNow();

        var existing = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (existing is null)
            return Result<RotatedTokens>.Failure(AuthErrors.InvalidRefreshToken());

        if (existing.IsRevoked)
        {
            await NukeUserSessionsAsync(existing.UserId, now, cancellationToken);
            return Result<RotatedTokens>.Failure(AuthErrors.RefreshTokenReused());
        }

        if (existing.ExpiresAt <= now)
            return Result<RotatedTokens>.Failure(AuthErrors.RefreshTokenExpired());

        var user = await _userManager.FindByIdAsync(existing.UserId.ToString());
        if (user is null)
            return Result<RotatedTokens>.Failure(AuthErrors.InvalidRefreshToken());

        var roles = await _userManager.GetRolesAsync(user);
        var jwt = _tokenGenerator.Generate(user.Id, user.Email!, roles.ToArray());
        var newRefresh = await _issuer.IssueAsync(user.Id, jwt.Jti, cancellationToken);

        existing.IsRevoked = true;
        existing.RevokedAt = now;
        existing.ReplacedByTokenHash = RefreshTokenIssuer.HashToken(newRefresh.RawToken);

        return new RotatedTokens(jwt.AccessToken, newRefresh.RawToken, jwt.ExpiresAt);
    }

    private async Task NukeUserSessionsAsync(Guid userId, DateTimeOffset now, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var freshContext = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var active = await freshContext.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(ct);

        foreach (var t in active)
        {
            t.IsRevoked = true;
            t.RevokedAt = now;
        }

        await freshContext.SaveChangesAsync(ct);
    }
}