using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Abstractions;

public interface IUserRegistrar
{
    Task<Result<Guid>> RegisterAsync(
        string email,
        string fullName,
        string password,
        CancellationToken cancellationToken = default);
}