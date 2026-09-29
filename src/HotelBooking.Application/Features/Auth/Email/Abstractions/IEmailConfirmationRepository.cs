using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Identity;

namespace HotelBooking.Application.Features.Auth.Email.Abstractions;

public interface IEmailConfirmationRepository
{
    Task<EmailConfirmationToken?> GetTokenWithUserByHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task<ApplicationUser?> GetUserByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<DateTimeOffset?> GetLastTokenIssuedAtAsync(Guid userId, CancellationToken cancellationToken);
}
