namespace HotelBooking.Domain.Entities;
public class RoomAvailability
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = default!;

    public DateOnly Date { get; set; }

    public Guid? BookingId { get; set; }
    public Booking? Booking { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
