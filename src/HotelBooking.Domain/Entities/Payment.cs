using HotelBooking.Domain.Common;

namespace HotelBooking.Domain.Entities;
public class Payment : BaseEntity
{
    public Guid BookingGroupId { get; set; }
    public BookingGroup BookingGroup { get; set; } = default!;

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Provider { get; set; } = "Stripe";
    public string? ProviderTransactionId { get; set; }
    public string IdempotencyKey { get; set; } = default!;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTimeOffset? ProcessedAt { get; set; }
}
