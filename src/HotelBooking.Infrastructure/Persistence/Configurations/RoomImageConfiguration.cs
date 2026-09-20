using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class RoomImageConfiguration : IEntityTypeConfiguration<RoomImage>
{
    public void Configure(EntityTypeBuilder<RoomImage> builder)
    {
        builder.Property(i => i.Url).IsRequired().HasMaxLength(500);
        builder.Property(i => i.PublicId).IsRequired().HasMaxLength(200);

        builder.HasOne(i => i.Room)
            .WithMany(r => r.Images)
            .HasForeignKey(i => i.RoomId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}