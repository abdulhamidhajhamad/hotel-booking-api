using System.ComponentModel.DataAnnotations;
using HotelBooking.Domain.Common;

namespace HotelBooking.Domain.Entities;
public class Room : BaseEntity
{
    public Guid HotelId { get; set; }
    public Hotel Hotel { get; set; } = default!;

    public Guid RoomTypeId { get; set; }
    public RoomType RoomType { get; set; } = default!;

    public string Number { get; set; } = default!;
    public int AdultsCapacity { get; set; }
    public int ChildrenCapacity { get; set; }
    public decimal PricePerNight { get; set; }
    public bool IsActive { get; set; } = true;

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    public ICollection<RoomImage> Images { get; set; } = new List<RoomImage>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<RoomAvailability> Availability { get; set; } = new List<RoomAvailability>();
    public ICollection<Discount> Discounts { get; set; } = new List<Discount>();
}
