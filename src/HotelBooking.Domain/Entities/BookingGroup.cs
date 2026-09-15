using System.ComponentModel.DataAnnotations;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Identity;

namespace HotelBooking.Domain.Entities;
public class BookingGroup : BaseEntity
{
    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public string ConfirmationNumber { get; set; } = default!;
    public decimal TotalPrice { get; set; }
    public string? SpecialRequests { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
