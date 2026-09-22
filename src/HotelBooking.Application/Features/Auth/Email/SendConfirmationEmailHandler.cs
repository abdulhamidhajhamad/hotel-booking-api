using HotelBooking.Application.Abstractions.Email;
using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Application.Common.Options;
using HotelBooking.Application.Features.Auth.Common;
using Microsoft.Extensions.Options;

namespace HotelBooking.Application.Features.Auth.EventHandlers;

public sealed class SendConfirmationEmailHandler : IOutboxHandler<UserRegisteredEvent>
{
    private readonly IEmailSender _emailSender;
    private readonly EmailConfirmationOptions _options;

    public SendConfirmationEmailHandler(
        IEmailSender emailSender,
        IOptions<EmailConfirmationOptions> options)
    {
        _emailSender = emailSender;
        _options = options.Value;
    }

    public Task HandleAsync(UserRegisteredEvent integrationEvent, CancellationToken cancellationToken)
    {
        var confirmUrl = _options.ConfirmUrlTemplate.Replace("{token}", integrationEvent.ConfirmationToken);
        var hours = _options.TokenLifetimeHours;

        var html = BuildHtml(integrationEvent.UserName, confirmUrl, hours);
        var text = BuildText(integrationEvent.UserName, confirmUrl, hours);

        var message = new EmailMessage(
            ToEmail: integrationEvent.Email,
            ToName: integrationEvent.UserName,
            Subject: "Confirm your Hotel Booking account",
            HtmlBody: html,
            PlainTextBody: text);

        return _emailSender.SendAsync(message, cancellationToken);
    }

    private static string BuildHtml(string userName, string confirmUrl, int hours)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <body style="font-family: sans-serif; line-height: 1.5;">
              <h2>Welcome, {{userName}}</h2>
              <p>Thanks for signing up with Hotel Booking. Please confirm your email to activate your account.</p>
              <p><a href="{{confirmUrl}}" style="display:inline-block;padding:10px 16px;background:#0066cc;color:#fff;text-decoration:none;border-radius:4px;">Confirm my email</a></p>
              <p>If the button does not work, copy this link into your browser:</p>
              <p><code>{{confirmUrl}}</code></p>
              <p>This link expires in {{hours}} hours.</p>
            </body>
            </html>
            """;
    }

    private static string BuildText(string userName, string confirmUrl, int hours)
    {
        return $"Welcome, {userName}.\n\nConfirm your email:\n{confirmUrl}\n\nLink expires in {hours} hours.";
    }
}
