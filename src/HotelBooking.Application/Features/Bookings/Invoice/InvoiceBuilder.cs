using HotelBooking.Application.Features.Bookings.Invoice.Abstractions;

namespace HotelBooking.Application.Features.Bookings.Invoice;

public sealed class InvoiceBuilder
{
    private readonly IInvoiceReader _reader;

    public InvoiceBuilder(IInvoiceReader reader) => _reader = reader;

    public async Task<InvoiceModel?> BuildAsync(Guid bookingGroupId, CancellationToken cancellationToken)
    {
        var group = await _reader.GetInvoiceDataAsync(bookingGroupId, cancellationToken);

        if (group is null)
            return null;

        var lines = group.Lines
            .Select(l => new InvoiceLine(
                l.HotelName,
                l.HotelAddress,
                l.RoomNumber,
                l.RoomType,
                l.CheckInDate,
                l.CheckOutDate,
                l.CheckOutDate.DayNumber - l.CheckInDate.DayNumber,
                l.OriginalPricePerNightSnapshot,
                l.PricePerNightSnapshot,
                l.OriginalPricePerNightSnapshot - l.PricePerNightSnapshot,
                l.TotalPrice))
            .ToList();

        return new InvoiceModel(
            group.Id,
            group.ConfirmationNumber,
            group.GuestName ?? string.Empty,
            group.GuestEmail ?? string.Empty,
            group.CreatedAt,
            group.PaymentStatus.ToString(),
            group.Currency ?? "USD",
            group.TotalPrice,
            lines);
    }
}
