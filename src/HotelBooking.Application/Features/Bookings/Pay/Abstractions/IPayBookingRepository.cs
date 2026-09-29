using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Bookings.Pay.Abstractions;

public interface IPayBookingRepository
{
    Task<BookingGroup?> GetGroupWithBookingsAndPaymentsAsync(Guid bookingGroupId, CancellationToken cancellationToken);

    void AddPayment(Payment payment);

    Task RemoveHoldsForBookingsAsync(IReadOnlyCollection<Guid> bookingIds, CancellationToken cancellationToken);
}
