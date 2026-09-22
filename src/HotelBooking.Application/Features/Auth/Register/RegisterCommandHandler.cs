
using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Auth.Abstractions;
using HotelBooking.Application.Features.Auth.Common;

namespace HotelBooking.Application.Features.Auth.Register;

public sealed class RegisterCommandHandler
    : ICommandHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUserRegistrar _userRegistrar;
    private readonly IEmailConfirmationTokenIssuer _tokenIssuer;
    private readonly IOutbox _outbox;

    public RegisterCommandHandler(
        IUserRegistrar userRegistrar,
        IEmailConfirmationTokenIssuer tokenIssuer,
        IOutbox outbox)
    {
        _userRegistrar = userRegistrar;
        _tokenIssuer = tokenIssuer;
        _outbox = outbox;
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

        var rawToken = await _tokenIssuer.IssueAsync(result.Value, cancellationToken);

        await _outbox.EnqueueAsync(
            new UserRegisteredEvent(result.Value, command.Email, command.UserName, rawToken),
            cancellationToken);

        return new RegisterResponse(result.Value, command.Email, command.UserName);
    }
}
