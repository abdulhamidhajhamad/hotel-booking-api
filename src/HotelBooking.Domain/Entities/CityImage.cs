using HotelBooking.Domain.Common;

namespace HotelBooking.Domain.Entities;
public class CityImage : BaseEntity
{
    public Guid CityId { get; set; }
    public City City { get; set; } = default!;

    public string Url { get; set; } = default!;
    public string PublicId { get; set; } = default!;
    public bool IsPrimary { get; set; } = false;
}