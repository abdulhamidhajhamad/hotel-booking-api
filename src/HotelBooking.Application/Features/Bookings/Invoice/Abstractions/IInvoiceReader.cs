namespace HotelBooking.Application.Features.Bookings.Invoice.Abstractions;

public interface IInvoiceReader
{
    Task<Guid?> GetOwnerIdAsync(Guid bookingGroupId, CancellationToken cancellationToken);

    Task<InvoiceRaw?> GetInvoiceDataAsync(Guid bookingGroupId, CancellationToken cancellationToken);
}
