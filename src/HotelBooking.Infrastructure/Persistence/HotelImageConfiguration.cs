using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class HotelImageConfiguration : IEntityTypeConfiguration<HotelImage>
{
    public void Configure(EntityTypeBuilder<HotelImage> builder)
    {
        builder.Property(i => i.Url).IsRequired().HasMaxLength(500);
        builder.Property(i => i.PublicId).IsRequired().HasMaxLength(200);

        builder.HasOne(i => i.Hotel)
            .WithMany(h => h.Images)
            .HasForeignKey(i => i.HotelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}