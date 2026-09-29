using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Users.Common;

namespace HotelBooking.Application.Features.Auth.Abstractions;

public interface IAdminUserCreator
{
    Task<Result<AdminUserDto>> CreateAsync(
        string email,
        string userName,
        string password,
        string role,
        CancellationToken cancellationToken = default);
}
