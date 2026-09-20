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
        string password,
        CancellationToken cancellationToken = default)
    {
        var existing = await _userManager.FindByEmailAsync(email);
        if (existing is not null)
            return Result<Guid>.Failure(AuthErrors.EmailAlreadyRegistered(email));

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email,
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