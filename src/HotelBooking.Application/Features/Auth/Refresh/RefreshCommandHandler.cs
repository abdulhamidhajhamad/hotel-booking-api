using FluentValidation;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Auth.Abstractions;

namespace HotelBooking.Application.Features.Auth.Refresh;

public sealed class RefreshCommandHandler
    : ICommandHandler<RefreshCommand, RefreshResponse>
{
    private readonly IRefreshTokenRotator _rotator;
    private readonly IValidator<RefreshCommand> _validator;
    private readonly TimeProvider _timeProvider;

    public RefreshCommandHandler(
        IRefreshTokenRotator rotator,
        IValidator<RefreshCommand> validator,
        TimeProvider timeProvider)
    {
        _rotator = rotator;
        _validator = validator;
        _timeProvider = timeProvider;
    }

    public async Task<Result<RefreshResponse>> Handle(
        RefreshCommand command,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(command, cancellationToken);
        if (!validation.IsValid)
        {
            var message = string.Join("; ",
                validation.Errors.Select(f => $"{f.PropertyName}: {f.ErrorMessage}"));
            return Result<RefreshResponse>.Failure(
                Error.Validation("Validation.Failed", message));
        }

        var rotated = await _rotator.RotateAsync(command.RefreshToken, cancellationToken);
        if (rotated.IsFailure)
            return Result<RefreshResponse>.Failure(rotated.Error);

        var expiresIn = (int)(rotated.Value.AccessExpiresAt - _timeProvider.GetUtcNow()).TotalSeconds;
        return new RefreshResponse(rotated.Value.AccessToken, rotated.Value.RefreshToken, expiresIn);
    }
}