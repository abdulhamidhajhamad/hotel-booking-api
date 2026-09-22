using System.Security.Cryptography;
using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Auth.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler : ICommandHandler<ConfirmEmailCommand>
{
    private readonly IApplicationDbContext _db;
    private readonly TimeProvider _clock;

    public ConfirmEmailCommandHandler(IApplicationDbContext db, TimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<Result> Handle(ConfirmEmailCommand command, CancellationToken cancellationToken)
    {
        byte[] rawBytes;
        try
        {
            rawBytes = Base64UrlDecode(command.Token);
        }
        catch (FormatException)
        {
            return Result.Failure(AuthErrors.InvalidOrExpiredToken());
        }

        var hash = Convert.ToBase64String(SHA256.HashData(rawBytes));
        var now = _clock.GetUtcNow();

        var token = await _db.EmailConfirmationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (token is null
            || token.UsedAtUtc is not null
            || token.InvalidatedAtUtc is not null
            || token.ExpiresAtUtc <= now)
        {
            return Result.Failure(AuthErrors.InvalidOrExpiredToken());
        }

        token.User.EmailConfirmed = true;
        token.UsedAtUtc = now;

        return Result.Success();
    }

    private static byte[] Base64UrlDecode(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }
}
