namespace HotelBooking.Application.Abstractions.Email;

public sealed record EmailAttachment(
    string FileName,
    byte[] Content,
    string ContentType);

public sealed record EmailMessage(
    string ToEmail,
    string ToName,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null,
    IReadOnlyList<EmailAttachment>? Attachments = null);