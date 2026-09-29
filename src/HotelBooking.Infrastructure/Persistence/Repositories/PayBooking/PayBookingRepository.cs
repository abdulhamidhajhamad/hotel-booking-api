using HotelBooking.Application.Features.Bookings.Pay.Abstractions;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.PayBooking;

public sealed class PayBookingRepository : IPayBookingRepository
{
    private readonly ApplicationDbContext _db;

    public PayBookingRepository(ApplicationDbContext db) => _db = db;

    public Task<BookingGroup?> GetGroupWithBookingsAndPaymentsAsync(Guid bookingGroupId, CancellationToken cancellationToken) =>
        _db.BookingGroups
            .Include(g => g.Bookings)
            .Include(g => g.Payments)
            .FirstOrDefaultAsync(g => g.Id == bookingGroupId, cancellationToken);

    public void AddPayment(Payment payment) => _db.Payments.Add(payment);

    public async Task RemoveHoldsForBookingsAsync(IReadOnlyCollection<Guid> bookingIds, CancellationToken cancellationToken)
    {
        var holds = await _db.RoomAvailability
            .Where(a => a.BookingId != null && bookingIds.Contains(a.BookingId.Value))
            .ToListAsync(cancellationToken);

        _db.RoomAvailability.RemoveRange(holds);
    }
}
