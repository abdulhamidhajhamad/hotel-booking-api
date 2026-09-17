using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Auth.Abstractions;

public interface IUserRegistrar
{
    Task<Result<Guid>> RegisterAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default);
}