using HotelBooking.Domain.Common;

namespace HotelBooking.Domain.Entities;

public class Discount : BaseEntity
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = default!;

    public string? Title { get; set; }
    public decimal Percentage { get; set; }
    public DateTime StartUtc { get; set; }
    public DateTime EndUtc { get; set; }
}