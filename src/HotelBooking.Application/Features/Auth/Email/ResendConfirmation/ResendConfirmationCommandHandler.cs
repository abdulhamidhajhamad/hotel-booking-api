using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Options;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Auth.Abstractions;
using HotelBooking.Application.Features.Auth.Common;
using HotelBooking.Application.Features.Auth.Email.Abstractions;
using Microsoft.Extensions.Options;

namespace HotelBooking.Application.Features.Auth.ResendConfirmation;

public sealed class ResendConfirmationCommandHandler : ICommandHandler<ResendConfirmationCommand>
{
    private readonly IEmailConfirmationRepository _emailConfirmation;
    private readonly IEmailConfirmationTokenIssuer _tokenIssuer;
    private readonly IOutbox _outbox;
    private readonly EmailConfirmationOptions _options;
    private readonly TimeProvider _clock;

    public ResendConfirmationCommandHandler(
        IEmailConfirmationRepository emailConfirmation,
        IEmailConfirmationTokenIssuer tokenIssuer,
        IOutbox outbox,
        IOptions<EmailConfirmationOptions> options,
        TimeProvider clock)
    {
        _emailConfirmation = emailConfirmation;
        _tokenIssuer = tokenIssuer;
        _outbox = outbox;
        _options = options.Value;
        _clock = clock;
    }

    public async Task<Result> Handle(ResendConfirmationCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.Trim().ToUpperInvariant();

        var user = await _emailConfirmation.GetUserByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || user.EmailConfirmed)
            return Result.Success();

        var now = _clock.GetUtcNow();
        var cooldown = TimeSpan.FromSeconds(_options.ResendCooldownSeconds);

        var lastIssuedAt = await _emailConfirmation.GetLastTokenIssuedAtAsync(user.Id, cancellationToken);

        if (lastIssuedAt is not null && (now - lastIssuedAt.Value) < cooldown)
            return Result.Success();

        var rawToken = await _tokenIssuer.IssueAsync(user.Id, cancellationToken);

        await _outbox.EnqueueAsync(
            new UserRegisteredEvent(user.Id, user.Email!, user.UserName!, rawToken),
            cancellationToken);

        return Result.Success();
    }
}
