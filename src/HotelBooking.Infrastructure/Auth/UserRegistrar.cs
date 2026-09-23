using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Auth.Abstractions;
using HotelBooking.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace HotelBooking.Infrastructure.Identity;

public sealed class UserRegistrar : IUserRegistrar
{
    private const string DefaultRole = "User";

    private readonly UserManager<ApplicationUser> _userManager;

    public UserRegistrar(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<Result<Guid>> RegisterAsync(
        string email,
        string userName,
        string password,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();
        var normalizedUserName = userName.Trim();

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
            return Result<Guid>.Failure(AuthErrors.RegistrationFailed(reason));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, DefaultRole);
        if (!roleResult.Succeeded)
        {
            var reason = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            return Result<Guid>.Failure(AuthErrors.RegistrationFailed(reason));
        }

        return Result<Guid>.Success(user.Id);
    }
}