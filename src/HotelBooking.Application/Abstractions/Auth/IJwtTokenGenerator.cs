namespace HotelBooking.Application.Features.Auth.Abstractions;

public interface IJwtTokenGenerator
{
    JwtToken Generate(Guid userId, string email, IReadOnlyList<string> roles);
}

public sealed record JwtToken(string AccessToken, string Jti, DateTimeOffset ExpiresAt);