using System.Security.Cryptography;
using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Features.Auth.Abstractions;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Email.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HotelBooking.Infrastructure.Identity;

public sealed class EmailConfirmationTokenIssuer : IEmailConfirmationTokenIssuer
{
    private readonly IApplicationDbContext _db;
    private readonly EmailConfirmationOptions _options;
    private readonly TimeProvider _clock;

    public EmailConfirmationTokenIssuer(
        IApplicationDbContext db,
        IOptions<EmailConfirmationOptions> options,
        TimeProvider clock)
    {
        _db = db;
        _options = options.Value;
        _clock = clock;
    }

    public async Task<string> IssueAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = _clock.GetUtcNow();

        var activeTokens = await _db.EmailConfirmationTokens
            .Where(t => t.UserId == userId
                     && t.UsedAtUtc == null
                     && t.InvalidatedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
            token.InvalidatedAtUtc = now;

        var rawBytes = RandomNumberGenerator.GetBytes(_options.TokenByteLength);
        var rawToken = Base64UrlEncode(rawBytes);
        var hash = SHA256.HashData(rawBytes);
        var hashString = Convert.ToBase64String(hash);

        _db.EmailConfirmationTokens.Add(new EmailConfirmationToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = hashString,
            ExpiresAtUtc = now.AddHours(_options.TokenLifetimeHours),
            CreatedAtUtc = now,
        });

        return rawToken;
    }

    private static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
