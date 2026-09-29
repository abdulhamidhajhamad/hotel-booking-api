using HotelBooking.Application.Features.Bookings.Invoice.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.Invoices;

public sealed class InvoiceReader : IInvoiceReader
{
    private readonly ApplicationDbContext _db;

    public InvoiceReader(ApplicationDbContext db) => _db = db;

    public Task<Guid?> GetOwnerIdAsync(Guid bookingGroupId, CancellationToken cancellationToken) =>
        _db.BookingGroups
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(g => g.Id == bookingGroupId)
            .Select(g => (Guid?)g.UserId)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<InvoiceRaw?> GetInvoiceDataAsync(Guid bookingGroupId, CancellationToken cancellationToken) =>
        _db.BookingGroups
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(g => g.Id == bookingGroupId)
            .Select(g => new InvoiceRaw(
                g.Id,
                g.ConfirmationNumber,
                g.TotalPrice,
                g.CreatedAt,
                g.User.FullName ?? g.User.UserName,
                g.User.Email,
                g.Payments
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => p.Status)
                    .FirstOrDefault(),
                g.Payments
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => p.Currency)
                    .FirstOrDefault(),
                g.Bookings.Select(b => new InvoiceLineRaw(
                    b.Room.Hotel.Name,
                    b.Room.Hotel.Address,
                    b.Room.Number,
                    b.Room.RoomType.Name,
                    b.CheckInDate,
                    b.CheckOutDate,
                    b.OriginalPricePerNightSnapshot,
                    b.PricePerNightSnapshot,
                    b.TotalPrice))
                    .ToList()))
            .FirstOrDefaultAsync(cancellationToken);
}
