using HotelBooking.Application.Common.Errors;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Users.Common;
using HotelBooking.Application.Features.Auth.Abstractions;
using HotelBooking.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace HotelBooking.Application.Features.Admin.Users.Create;

public sealed class CreateAdminUserCommandHandler
    : ICommandHandler<CreateAdminUserCommand, AdminUserDto>
{
    private readonly IAdminUserCreator _creator;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateAdminUserCommandHandler(
        IAdminUserCreator creator,
        UserManager<ApplicationUser> userManager)
    {
        _creator = creator;
        _userManager = userManager;
    }

    public async Task<Result<AdminUserDto>> Handle(
        CreateAdminUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _creator.CreateAsync(
            command.Email,
            command.UserName,
            command.Password,
            command.Role,
            cancellationToken);

        if (result.IsFailure)
            return Result<AdminUserDto>.Failure(result.Error);

        var user = await _userManager.FindByIdAsync(result.Value.ToString());
        if (user is null)
            return Result<AdminUserDto>.Failure(AuthErrors.RegistrationFailed("User was created but could not be loaded."));

        return new AdminUserDto(
            user.Id,
            user.Email!,
            user.UserName!,
            command.Role,
            user.CreatedAt);
    }
}