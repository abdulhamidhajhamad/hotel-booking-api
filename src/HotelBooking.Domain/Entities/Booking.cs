using System.ComponentModel.DataAnnotations;
using HotelBooking.Domain.Common;

namespace HotelBooking.Domain.Entities;
public class Booking : BaseEntity
{
    public Guid BookingGroupId { get; set; }
    public BookingGroup BookingGroup { get; set; } = default!;

    public Guid RoomId { get; set; }
    public Room Room { get; set; } = default!;

    public DateOnly CheckInDate { get; set; }
    public DateOnly CheckOutDate { get; set; }
    public int AdultsCount { get; set; }
    public int ChildrenCount { get; set; }
    public decimal PricePerNightSnapshot { get; set; }
    public decimal OriginalPricePerNightSnapshot { get; set; }
    public decimal TotalPrice { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    public ICollection<RoomAvailability> AvailabilityHolds { get; set; } = new List<RoomAvailability>();
}
