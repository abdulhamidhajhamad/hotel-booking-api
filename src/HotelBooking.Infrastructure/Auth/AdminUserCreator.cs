using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Users.Common;
using HotelBooking.Application.Features.Auth.Abstractions;
using HotelBooking.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace HotelBooking.Infrastructure.Identity;

public sealed class AdminUserCreator : IAdminUserCreator
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public AdminUserCreator(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public async Task<Result<Guid>> CreateAsync(
        string email,
        string userName,
        string password,
        string role,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();
        var normalizedUserName = userName.Trim();

        if (!await _roleManager.RoleExistsAsync(role))
            return Result<Guid>.Failure(AdminUserErrors.InvalidRole(role));

        if (await _userManager.FindByEmailAsync(normalizedEmail) is not null)
            return Result<Guid>.Failure(AuthErrors.EmailAlreadyRegistered(normalizedEmail));

        if (await _userManager.FindByNameAsync(normalizedUserName) is not null)
            return Result<Guid>.Failure(AuthErrors.UsernameAlreadyTaken(normalizedUserName));

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = normalizedUserName,
            Email = normalizedEmail,
        };

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            var reason = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return Result<Guid>.Failure(AdminUserErrors.CreationFailed(reason));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            var reason = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            return Result<Guid>.Failure(AdminUserErrors.CreationFailed(reason));
        }

        return Result<Guid>.Success(user.Id);
    }
}