using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Admin.Users.Common;

namespace HotelBooking.Application.Features.Admin.Users.Create;

public sealed record CreateAdminUserCommand(
    string Email,
    string UserName,
    string Password,
    string Role) : ICommand<AdminUserDto>;