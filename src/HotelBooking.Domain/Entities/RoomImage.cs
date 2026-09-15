using HotelBooking.Domain.Common;

namespace HotelBooking.Domain.Entities;
public class RoomImage : BaseEntity
{
    public Guid RoomId { get; set; }
    public Room Room { get; set; } = default!;

    public string Url { get; set; } = default!;
    public string PublicId { get; set; } = default!;
}