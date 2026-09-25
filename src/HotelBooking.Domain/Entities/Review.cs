using HotelBooking.Domain.Common;
using HotelBooking.Domain.Identity;

namespace HotelBooking.Domain.Entities;
public class Review : BaseEntity
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = default!;

    public Guid HotelId { get; set; }
    public Hotel Hotel { get; set; } = default!;

    public Guid UserId { get; set; }
    public ApplicationUser User { get; set; } = default!;

    public int Rating { get; set; }
    public string? Comment { get; set; }
}