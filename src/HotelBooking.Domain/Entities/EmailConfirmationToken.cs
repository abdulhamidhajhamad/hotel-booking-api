using HotelBooking.Domain.Identity;

namespace HotelBooking.Domain.Entities;

public sealed class EmailConfirmationToken
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid UserId { get; init; }
    public string TokenHash { get; init; } = default!;
    public DateTimeOffset ExpiresAtUtc { get; init; }
    public DateTimeOffset? UsedAtUtc { get; set; }
    public DateTimeOffset? InvalidatedAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public ApplicationUser User { get; init; } = default!;
}
