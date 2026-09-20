using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Common.Errors;

public static class AuthErrors
{
    public static Error EmailAlreadyRegistered(string email) =>
        Error.Conflict(
            "Auth.EmailAlreadyRegistered",
            $"An account with email '{email}' already exists.");

    public static Error PasswordTooWeak(string reason) =>
        Error.Validation(
            "Auth.PasswordTooWeak",
            reason);

    public static Error RegistrationFailed(string reason) =>
        Error.Failure(
            "Auth.RegistrationFailed",
            reason);
}