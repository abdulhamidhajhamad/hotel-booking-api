using HotelBooking.Application.Abstractions.Email;
using HotelBooking.Application.Abstractions.Invoicing;
using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Application.Features.Bookings.Common;
using HotelBooking.Application.Features.Bookings.Invoice;

namespace HotelBooking.Application.Features.Bookings.EventHandlers;

public sealed class SendInvoiceEmailHandler : IOutboxHandler<BookingConfirmedEvent>
{
    private readonly InvoiceBuilder _invoiceBuilder;
    private readonly IInvoiceRenderer _invoiceRenderer;
    private readonly IEmailSender _emailSender;

    public SendInvoiceEmailHandler(
        InvoiceBuilder invoiceBuilder,
        IInvoiceRenderer invoiceRenderer,
        IEmailSender emailSender)
    {
        _invoiceBuilder = invoiceBuilder;
        _invoiceRenderer = invoiceRenderer;
        _emailSender = emailSender;
    }

    public async Task HandleAsync(BookingConfirmedEvent integrationEvent, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceBuilder.BuildAsync(integrationEvent.BookingGroupId, cancellationToken);
        if (invoice is null || string.IsNullOrWhiteSpace(invoice.GuestEmail))
            return;

        var pdf = _invoiceRenderer.RenderPdf(invoice);

        var attachment = new EmailAttachment(
            FileName: $"invoice-{invoice.ConfirmationNumber}.pdf",
            Content: pdf,
            ContentType: "application/pdf");

        var message = new EmailMessage(
            ToEmail: invoice.GuestEmail,
            ToName: invoice.GuestName,
            Subject: $"Your booking confirmation {invoice.ConfirmationNumber}",
            HtmlBody: BuildHtml(invoice),
            PlainTextBody: BuildText(invoice),
            Attachments: new[] { attachment });

        await _emailSender.SendAsync(message, cancellationToken);
    }

    private static string BuildHtml(InvoiceModel invoice)
    {
        return $$"""
            <!DOCTYPE html>
            <html>
            <body style="font-family: sans-serif; line-height: 1.5;">
              <h2>Thank you, {{invoice.GuestName}}</h2>
              <p>Your booking is confirmed. Here is a summary:</p>
              <ul>
                <li><strong>Confirmation number:</strong> {{invoice.ConfirmationNumber}}</li>
                <li><strong>Payment status:</strong> {{invoice.PaymentStatus}}</li>
                <li><strong>Total paid:</strong> {{invoice.TotalPrice:0.00}} {{invoice.Currency}}</li>
              </ul>
              <p>Your full invoice is attached to this email as a PDF.</p>
            </body>
            </html>
            """;
    }

    private static string BuildText(InvoiceModel invoice)
    {
        return $"Thank you, {invoice.GuestName}.\n\n"
             + $"Your booking is confirmed.\n"
             + $"Confirmation number: {invoice.ConfirmationNumber}\n"
             + $"Payment status: {invoice.PaymentStatus}\n"
             + $"Total paid: {invoice.TotalPrice:0.00} {invoice.Currency}\n\n"
             + "Your full invoice is attached as a PDF.";
    }
}