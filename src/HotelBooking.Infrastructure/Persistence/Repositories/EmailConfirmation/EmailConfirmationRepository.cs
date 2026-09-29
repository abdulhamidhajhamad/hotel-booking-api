using HotelBooking.Application.Features.Auth.Email.Abstractions;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.EmailConfirmation;

public sealed class EmailConfirmationRepository : IEmailConfirmationRepository
{
    private readonly ApplicationDbContext _db;

    public EmailConfirmationRepository(ApplicationDbContext db) => _db = db;

    public Task<EmailConfirmationToken?> GetTokenWithUserByHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        _db.EmailConfirmationTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);

    public Task<ApplicationUser?> GetUserByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        _db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

    public Task<DateTimeOffset?> GetLastTokenIssuedAtAsync(Guid userId, CancellationToken cancellationToken) =>
        _db.EmailConfirmationTokens
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => (DateTimeOffset?)t.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
}
