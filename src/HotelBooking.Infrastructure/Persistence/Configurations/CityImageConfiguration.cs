using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class CityImageConfiguration : IEntityTypeConfiguration<CityImage>
{
    public void Configure(EntityTypeBuilder<CityImage> builder)
    {
        builder.Property(i => i.Url).IsRequired().HasMaxLength(500);
        builder.Property(i => i.PublicId).IsRequired().HasMaxLength(200);

        builder.HasOne(i => i.City)
            .WithMany(c => c.Images)
            .HasForeignKey(i => i.CityId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}