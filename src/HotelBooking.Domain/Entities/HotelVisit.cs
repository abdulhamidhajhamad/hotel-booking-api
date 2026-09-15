using HotelBooking.Domain.Identity;

namespace HotelBooking.Domain.Entities;
public class HotelVisit
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public Guid HotelId { get; set; }
    public Hotel Hotel { get; set; } = default!;

    public DateTimeOffset ViewedAt { get; set; } = DateTimeOffset.UtcNow;
}
