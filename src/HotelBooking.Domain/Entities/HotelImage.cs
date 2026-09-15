using HotelBooking.Domain.Common;

namespace HotelBooking.Domain.Entities;
public class HotelImage : BaseEntity
{
    public Guid HotelId { get; set; }
    public Hotel Hotel { get; set; } = default!;

    public string Url { get; set; } = default!;
    public string PublicId { get; set; } = default!;
    public bool IsPrimary { get; set; } = false;
}