using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Auth.Abstractions;

public interface IUserAuthenticator
{
    Task<Result<AuthenticatedUser>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}

public sealed record AuthenticatedUser(Guid Id, string Email, IReadOnlyList<string> Roles);