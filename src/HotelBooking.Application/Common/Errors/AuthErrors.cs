using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Common.Errors;

public static class AuthErrors
{
    public static Error EmailAlreadyRegistered(string email) =>
        Error.Conflict(
            "Auth.EmailAlreadyRegistered",
            $"An account with email '{email}' already exists.");

    public static Error UsernameAlreadyTaken(string userName) =>
        Error.Conflict(
            "Auth.UsernameAlreadyTaken",
            $"The username '{userName}' is already taken.");

    public static Error PasswordTooWeak(string reason) =>
        Error.Validation("Auth.PasswordTooWeak", reason);

    public static Error RegistrationFailed(string reason) =>
        Error.Failure("Auth.RegistrationFailed", reason);

    public static Error InvalidCredentials() =>
        Error.Unauthorized("Auth.InvalidCredentials", "Email or password is incorrect.");

    public static Error AccountLockedOut() =>
        Error.Unauthorized("Auth.AccountLockedOut", "Account is temporarily locked due to failed login attempts.");

    public static Error InvalidRefreshToken() =>
        Error.Unauthorized("Auth.InvalidRefreshToken", "The refresh token is invalid.");

    public static Error RefreshTokenExpired() =>
        Error.Unauthorized("Auth.RefreshTokenExpired", "The refresh token has expired.");

    public static Error RefreshTokenReused() =>
        Error.Unauthorized("Auth.RefreshTokenReused",
            "The refresh token has already been used. All sessions for this account have been revoked.");

    public static Error InvalidOrExpiredToken() =>
        Error.Validation("Auth.InvalidOrExpiredToken",
            "The confirmation token is invalid, expired, or already used.");
}
