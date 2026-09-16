using Microsoft.AspNetCore.Identity;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Domain.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string? FullName { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<BookingGroup> BookingGroups { get; set; } = new List<BookingGroup>();
    public ICollection<HotelVisit> HotelVisits { get; set; } = new List<HotelVisit>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}