using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Auth.Abstractions;

namespace HotelBooking.Application.Features.Auth.Register;

public sealed class RegisterCommandHandler
    : ICommandHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUserRegistrar _userRegistrar;

    public RegisterCommandHandler(IUserRegistrar userRegistrar)
    {
        _userRegistrar = userRegistrar;
    }

    public async Task<Result<RegisterResponse>> Handle(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _userRegistrar.RegisterAsync(
            command.Email,
            command.UserName,
            command.Password,
            cancellationToken);

        if (result.IsFailure)
            return Result<RegisterResponse>.Failure(result.Error);

        return new RegisterResponse(result.Value, command.Email, command.UserName);
    }
}