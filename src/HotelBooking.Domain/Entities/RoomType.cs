using HotelBooking.Domain.Common;

namespace HotelBooking.Domain.Entities;
public class RoomType : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
}
