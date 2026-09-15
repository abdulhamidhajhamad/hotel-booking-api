using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.Property(r => r.Number).IsRequired().HasMaxLength(20);
        builder.Property(r => r.PricePerNight).HasColumnType("decimal(18,2)");
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.HasOne(r => r.Hotel)
            .WithMany(h => h.Rooms)
            .HasForeignKey(r => r.HotelId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.RoomType)
            .WithMany(t => t.Rooms)
            .HasForeignKey(r => r.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.HotelId, r.Number }).IsUnique();

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Room_AdultsCapacity", "[AdultsCapacity] >= 1");
            t.HasCheckConstraint("CK_Room_ChildrenCapacity", "[ChildrenCapacity] >= 0");
            t.HasCheckConstraint("CK_Room_PricePerNight", "[PricePerNight] >= 0");
        });
    }
}
