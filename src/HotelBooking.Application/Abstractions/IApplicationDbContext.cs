using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Abstractions;

public interface IApplicationDbContext
{
    DbSet<City> Cities { get; }
    DbSet<Hotel> Hotels { get; }
    DbSet<HotelImage> HotelImages { get; }
    DbSet<Amenity> Amenities { get; }
    DbSet<HotelAmenity> HotelAmenities { get; }
    DbSet<Room> Rooms { get; }
    DbSet<RoomType> RoomTypes { get; }
    DbSet<RoomImage> RoomImages { get; }
    DbSet<RoomAvailability> RoomAvailability { get; }
    DbSet<BookingGroup> BookingGroups { get; }
    DbSet<Booking> Bookings { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Discount> Discounts { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<EmailConfirmationToken> EmailConfirmationTokens { get; }
    DbSet<Domain.Identity.ApplicationUser> Users { get; }
    DatabaseFacade Database { get; }
	DbSet<CityImage> CityImages { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }
    DbSet<Review> Reviews { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
