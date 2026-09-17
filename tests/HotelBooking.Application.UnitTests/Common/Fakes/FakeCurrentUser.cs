using HotelBooking.Application.Abstractions;

namespace HotelBooking.Application.UnitTests.Common.Fakes;

public sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? Id { get; set; }
    public string? Email { get; set; }
    public bool IsAuthenticated => Id is not null;
    public IReadOnlyList<string> Permissions { get; set; } = Array.Empty<string>();
    public string? Jti { get; set; }
    public DateTimeOffset? AccessTokenExpiresAt { get; set; }

    public static FakeCurrentUser SignedIn(
        Guid userId,
        string jti = "test-jti",
        DateTimeOffset? expiresAt = null) =>
        new()
        {
            Id = userId,
            Email = $"{userId}@test.local",
            Jti = jti,
            AccessTokenExpiresAt = expiresAt ?? DateTimeOffset.UtcNow.AddMinutes(15)
        };

    public static FakeCurrentUser Anonymous() => new();
}