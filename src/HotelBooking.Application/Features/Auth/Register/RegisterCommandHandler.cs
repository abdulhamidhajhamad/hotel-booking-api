using FluentValidation;
using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Auth.Register;

public sealed class RegisterCommandHandler
    : ICommandHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUserRegistrar _userRegistrar;
    private readonly IValidator<RegisterCommand> _validator;

    public RegisterCommandHandler(
        IUserRegistrar userRegistrar,
        IValidator<RegisterCommand> validator)
    {
        _userRegistrar = userRegistrar;
        _validator = validator;
    }

    public async Task<Result<RegisterResponse>> Handle(
        RegisterCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var message = string.Join("; ",
                validation.Errors.Select(f => $"{f.PropertyName}: {f.ErrorMessage}"));
            return Result<RegisterResponse>.Failure(
                Error.Validation("Validation.Failed", message));
        }

        var result = await _userRegistrar.RegisterAsync(
            command.Email,
            command.Password,
            cancellationToken);

        if (result.IsFailure)
            return Result<RegisterResponse>.Failure(result.Error);

        return new RegisterResponse(result.Value, command.Email);
    }
}