using HotelBooking.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Bookings.Invoice;

public sealed class InvoiceBuilder
{
    private readonly IApplicationDbContext _db;

    public InvoiceBuilder(IApplicationDbContext db) => _db = db;

    public async Task<InvoiceModel?> BuildAsync(Guid bookingGroupId, CancellationToken cancellationToken)
    {
        var group = await _db.BookingGroups
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(g => g.Id == bookingGroupId)
            .Select(g => new
            {
                g.Id,
                g.ConfirmationNumber,
                g.TotalPrice,
                g.CreatedAt,
                GuestName = g.User.FullName ?? g.User.UserName,
                GuestEmail = g.User.Email,
                PaymentStatus = g.Payments
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => p.Status)
                    .FirstOrDefault(),
                Currency = g.Payments
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => p.Currency)
                    .FirstOrDefault(),
                Lines = g.Bookings.Select(b => new
                {
                    HotelName = b.Room.Hotel.Name,
                    HotelAddress = b.Room.Hotel.Address,
                    RoomNumber = b.Room.Number,
                    RoomType = b.Room.RoomType.Name,
                    b.CheckInDate,
                    b.CheckOutDate,
                    b.OriginalPricePerNightSnapshot,
                    b.PricePerNightSnapshot,
                    b.TotalPrice
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

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