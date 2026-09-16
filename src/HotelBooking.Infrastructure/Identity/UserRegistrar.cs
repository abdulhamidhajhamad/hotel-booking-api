using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Results;
using HotelBooking.Domain.Identity;

namespace HotelBooking.Infrastructure.Identity;

public sealed class UserRegistrar : IUserRegistrar
{
    private const string DefaultRole = "User";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _dbContext;

    public UserRegistrar(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext dbContext)
    {
        _userManager = userManager;
        _dbContext = dbContext;
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

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            var reason = string.Join("; ", createResult.Errors.Select(e => e.Description));
            return Result<Guid>.Failure(AuthErrors.RegistrationFailed(reason));
        }

        var roleResult = await _userManager.AddToRoleAsync(user, DefaultRole);
        if (!roleResult.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            var reason = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            return Result<Guid>.Failure(AuthErrors.RegistrationFailed(reason));
        }

        await transaction.CommitAsync(cancellationToken);

        return user.Id;
    }
}