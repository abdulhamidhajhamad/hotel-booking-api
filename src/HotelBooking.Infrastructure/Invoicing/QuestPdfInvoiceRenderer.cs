using HotelBooking.Application.Abstractions.Invoicing;
using HotelBooking.Application.Features.Bookings.Invoice;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace HotelBooking.Infrastructure.Invoicing;

public sealed class QuestPdfInvoiceRenderer : IInvoiceRenderer
{
    public byte[] RenderPdf(InvoiceModel model)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Margin(40);
                page.Size(PageSizes.A4);
                page.DefaultTextStyle(x => x.FontSize(11));

                page.Header().Column(header =>
                {
                    header.Item().Text("Hotel Booking Invoice").FontSize(20).Bold();
                    header.Item().Text($"Confirmation: {model.ConfirmationNumber}");
                    header.Item().Text($"Issued: {model.IssuedAt:yyyy-MM-dd HH:mm} UTC");
                    header.Item().Text($"Guest: {model.GuestName} ({model.GuestEmail})");
                    header.Item().Text($"Payment status: {model.PaymentStatus}");
                });

                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(2);
                    });

                    table.Header(h =>
                    {
                        h.Cell().Text("Hotel / Room").Bold();
                        h.Cell().Text("Check-in").Bold();
                        h.Cell().Text("Check-out").Bold();
                        h.Cell().AlignRight().Text("Nights").Bold();
                        h.Cell().AlignRight().Text("Original/night").Bold();
                        h.Cell().AlignRight().Text("Discount/night").Bold();
                        h.Cell().AlignRight().Text("Line total").Bold();
                    });

                    foreach (var line in model.Lines)
                    {
                        table.Cell().Text($"{line.HotelName}\n{line.RoomType} #{line.RoomNumber}\n{line.HotelAddress}");
                        table.Cell().Text(line.CheckInDate.ToString("yyyy-MM-dd"));
                        table.Cell().Text(line.CheckOutDate.ToString("yyyy-MM-dd"));
                        table.Cell().AlignRight().Text(line.Nights.ToString());
                        table.Cell().AlignRight().Text($"{line.OriginalPricePerNight:0.00}");
                        table.Cell().AlignRight().Text($"{line.DiscountPerNight:0.00}");
                        table.Cell().AlignRight().Text($"{line.LineTotal:0.00}");
                    }
                });

                page.Footer().AlignRight().Text($"Total: {model.TotalPrice:0.00} {model.Currency}").FontSize(14).Bold();
            });
        });

        return document.GeneratePdf();
    }
}