using HotelBooking.Domain.Identity;

namespace HotelBooking.Domain.Entities;
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public string TokenHash { get; set; } = default!;
    public string Jti { get; set; } = default!;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
