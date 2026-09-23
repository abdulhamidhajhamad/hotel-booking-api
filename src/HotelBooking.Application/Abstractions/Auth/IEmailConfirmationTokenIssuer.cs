namespace HotelBooking.Application.Features.Auth.Abstractions;

public interface IEmailConfirmationTokenIssuer
{
    Task<string> IssueAsync(Guid userId, CancellationToken cancellationToken);
}
