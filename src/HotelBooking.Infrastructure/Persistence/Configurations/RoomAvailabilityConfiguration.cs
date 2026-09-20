using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class RoomAvailabilityConfiguration : IEntityTypeConfiguration<RoomAvailability>
{
    public void Configure(EntityTypeBuilder<RoomAvailability> builder)
    {
        builder.HasKey(a => a.Id);

        builder.HasOne(a => a.Room)
            .WithMany(r => r.Availability)
            .HasForeignKey(a => a.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.Booking)
            .WithMany(b => b.AvailabilityHolds)
            .HasForeignKey(a => a.BookingId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasIndex(a => new { a.RoomId, a.Date }).IsUnique();
    }
}
