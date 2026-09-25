using HotelBooking.Application.Features.Bookings.Invoice;

namespace HotelBooking.Application.Abstractions.Invoicing;

public interface IInvoiceRenderer
{
    byte[] RenderPdf(InvoiceModel model);
}