using HotelBooking.Domain.Common;

namespace HotelBooking.Domain.Entities;

public class IdempotencyRecord
{
    public string Key { get; set; } = default!;
    public string RequestHash { get; set; } = default!;
    public IdempotencyStatus Status { get; set; } = IdempotencyStatus.Pending;
    public Guid? BookingGroupId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAt { get; set; }
}