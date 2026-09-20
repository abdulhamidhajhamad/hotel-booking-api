namespace HotelBooking.Application.Abstractions;

public interface ICurrentUser
{
    Guid? Id { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
    IReadOnlyList<string> Permissions { get; }
    string? Jti { get; }
    DateTimeOffset? AccessTokenExpiresAt { get; }
}