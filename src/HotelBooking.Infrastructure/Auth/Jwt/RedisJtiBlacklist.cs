using StackExchange.Redis;
using HotelBooking.Application.Abstractions;

using HotelBooking.Application.Features.Auth.Abstractions;

namespace HotelBooking.Infrastructure.Identity;

public sealed class RedisJtiBlacklist : IJtiBlacklist
{
    private const string KeyPrefix = "jti:";
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeProvider _timeProvider;

    public RedisJtiBlacklist(IConnectionMultiplexer redis, TimeProvider timeProvider)
    {
        _redis = redis;
        _timeProvider = timeProvider;
    }

    public async Task AddAsync(
        string jti,
        DateTimeOffset absoluteExpiration,
        CancellationToken cancellationToken = default)
    {
        var ttl = absoluteExpiration - _timeProvider.GetUtcNow();
        if (ttl <= TimeSpan.Zero) return;

        var db = _redis.GetDatabase();
        await db.StringSetAsync(KeyPrefix + jti, "1", ttl);
    }

    public async Task<bool> IsBlacklistedAsync(
        string jti,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        return await db.KeyExistsAsync(KeyPrefix + jti);
    }
}