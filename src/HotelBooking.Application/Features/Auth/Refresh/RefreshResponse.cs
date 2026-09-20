namespace HotelBooking.Application.Features.Auth.Refresh;

public sealed record RefreshResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType = "Bearer");