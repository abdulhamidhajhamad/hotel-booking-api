using HotelBooking.Domain.Common;

namespace HotelBooking.Domain.Entities;
public class Amenity : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Icon { get; set; }

    public ICollection<HotelAmenity> HotelAmenities { get; set; } = new List<HotelAmenity>();
}