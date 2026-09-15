using HotelBooking.Domain.Common;
using HotelBooking.Domain.Identity;

namespace HotelBooking.Domain.Entities;
public class Hotel : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int StarRating { get; set; }
    public HotelCategory Category { get; set; } = HotelCategory.Standard;

    public string Address { get; set; } = default!;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public Guid CityId { get; set; }
    public City City { get; set; } = default!;

    public Guid? OwnerId { get; set; }
    public ApplicationUser? Owner { get; set; }

    public ICollection<Room> Rooms { get; set; } = new List<Room>();
    public ICollection<HotelImage> Images { get; set; } = new List<HotelImage>();
    public ICollection<HotelAmenity> HotelAmenities { get; set; } = new List<HotelAmenity>();
    public ICollection<HotelVisit> Visits { get; set; } = new List<HotelVisit>();
}
