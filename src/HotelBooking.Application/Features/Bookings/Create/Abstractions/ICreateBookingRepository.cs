using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Bookings.Create.Abstractions;

public interface ICreateBookingRepository
{
    Task<IdempotencyRecord?> GetIdempotencyRecordAsync(string key, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, RoomPricing>> GetRoomPricingAsync(IReadOnlyCollection<Guid> roomIds, CancellationToken cancellationToken);

    Task<IReadOnlyList<(Guid RoomId, DateOnly Date)>> GetTakenSlotsAsync(IReadOnlyCollection<Guid> roomIds, CancellationToken cancellationToken);

    Task<bool> TryPersistBookingAsync(BookingGroup group, IdempotencyRecord record, CancellationToken cancellationToken);

    Task<BookingGroupSnapshot?> GetBookingGroupSnapshotAsync(Guid bookingGroupId, CancellationToken cancellationToken);
}
