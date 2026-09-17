namespace HotelBooking.Application.Features.Auth.Abstractions;

public interface IJtiBlacklist
{
    Task AddAsync(
        string jti,
        DateTimeOffset absoluteExpiration,
        CancellationToken cancellationToken = default);

    Task<bool> IsBlacklistedAsync(
        string jti,
        CancellationToken cancellationToken = default);
}