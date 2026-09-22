using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Auth.Abstractions;

public interface IAdminUserCreator
{
    Task<Result<Guid>> CreateAsync(
        string email,
        string userName,
        string password,
        string role,
        CancellationToken cancellationToken = default);
}