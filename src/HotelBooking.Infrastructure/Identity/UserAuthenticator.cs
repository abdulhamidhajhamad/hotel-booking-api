using Microsoft.AspNetCore.Identity;
using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Results;
using HotelBooking.Domain.Identity;

using HotelBooking.Application.Features.Auth.Abstractions;

namespace HotelBooking.Infrastructure.Identity;

public sealed class UserAuthenticator : IUserAuthenticator
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserAuthenticator(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<AuthenticatedUser>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null)
            return Result<AuthenticatedUser>.Failure(AuthErrors.InvalidCredentials());

        if (await _userManager.IsLockedOutAsync(user))
            return Result<AuthenticatedUser>.Failure(AuthErrors.AccountLockedOut());

        if (!await _userManager.CheckPasswordAsync(user, password))
        {
            await _userManager.AccessFailedAsync(user);
            if (await _userManager.IsLockedOutAsync(user))
                return Result<AuthenticatedUser>.Failure(AuthErrors.AccountLockedOut());
            return Result<AuthenticatedUser>.Failure(AuthErrors.InvalidCredentials());
        }

        await _userManager.ResetAccessFailedCountAsync(user);
        var roles = await _userManager.GetRolesAsync(user);

        return new AuthenticatedUser(user.Id, user.Email!, roles.ToArray());
    }
}