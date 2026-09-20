using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.Property(b => b.PricePerNightSnapshot).HasColumnType("decimal(18,2)");
        builder.Property(b => b.TotalPrice).HasColumnType("decimal(18,2)");
        builder.Property(b => b.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(b => b.RowVersion).IsRowVersion();

        builder.HasOne(b => b.BookingGroup)
            .WithMany(g => g.Bookings)
            .HasForeignKey(b => b.BookingGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(b => b.Room)
            .WithMany(r => r.Bookings)
            .HasForeignKey(b => b.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Booking_Dates", "[CheckOutDate] > [CheckInDate]");
            t.HasCheckConstraint("CK_Booking_AdultsCount", "[AdultsCount] >= 1");
            t.HasCheckConstraint("CK_Booking_ChildrenCount", "[ChildrenCount] >= 0");
            t.HasCheckConstraint("CK_Booking_TotalPrice", "[TotalPrice] >= 0");
        });
    }
}
