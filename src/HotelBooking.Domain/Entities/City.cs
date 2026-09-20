using HotelBooking.Domain.Common;

namespace HotelBooking.Domain.Entities;
public class City : BaseEntity
{
    public string Name { get; set; } = default!;
    public string Country { get; set; } = default!;
    public string? PostalCode { get; set; }
    public string Timezone { get; set; } = default!;

    public ICollection<Hotel> Hotels { get; set; } = new List<Hotel>();
}
