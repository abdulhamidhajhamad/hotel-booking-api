using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.Property(a => a.Name).IsRequired().HasMaxLength(100);
        builder.Property(a => a.Icon).HasMaxLength(100);
        builder.HasIndex(a => a.Name).IsUnique();
    }
}
