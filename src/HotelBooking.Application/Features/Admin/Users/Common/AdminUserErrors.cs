using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Admin.Users.Common;

public static class AdminUserErrors
{
    public static Error InvalidRole(string role) =>
        Error.Validation(
            "AdminUsers.InvalidRole",
            $"Role '{role}' is not supported. Allowed roles: User, Admin.");

    public static Error CreationFailed(string reason) =>
        Error.Failure("AdminUsers.CreationFailed", reason);
}