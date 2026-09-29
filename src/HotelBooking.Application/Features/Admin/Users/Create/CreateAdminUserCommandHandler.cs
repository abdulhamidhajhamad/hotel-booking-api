using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Users.Common;
using HotelBooking.Application.Features.Auth.Abstractions;

namespace HotelBooking.Application.Features.Admin.Users.Create;

public sealed class CreateAdminUserCommandHandler
    : ICommandHandler<CreateAdminUserCommand, AdminUserDto>
{
    private readonly IAdminUserCreator _creator;

    public CreateAdminUserCommandHandler(IAdminUserCreator creator) => _creator = creator;

    public Task<Result<AdminUserDto>> Handle(
        CreateAdminUserCommand command,
        CancellationToken cancellationToken) =>
        _creator.CreateAsync(
            command.Email,
            command.UserName,
            command.Password,
            command.Role,
            cancellationToken);
}
