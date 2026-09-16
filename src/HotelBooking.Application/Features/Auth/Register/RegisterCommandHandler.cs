using MediatR;
using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Auth.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RegisterResponse>>
{
    private readonly IUserRegistrar _userRegistrar;

    public RegisterCommandHandler(IUserRegistrar userRegistrar)
    {
        _userRegistrar = userRegistrar;
    }

    public async Task<Result<RegisterResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var result = await _userRegistrar.RegisterAsync(
            request.Email,
            request.FullName,
            request.Password,
            cancellationToken);

        if (result.IsFailure)
            return Result<RegisterResponse>.Failure(result.Error);

        return new RegisterResponse(result.Value, request.Email);
    }
}