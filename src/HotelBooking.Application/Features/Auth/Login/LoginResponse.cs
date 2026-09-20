namespace HotelBooking.Application.Features.Auth.Login;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    string TokenType = "Bearer");