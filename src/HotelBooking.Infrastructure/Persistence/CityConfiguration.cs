using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Country).IsRequired().HasMaxLength(2).IsFixedLength();
        builder.Property(c => c.PostalCode).HasMaxLength(20);
        builder.Property(c => c.Timezone).IsRequired().HasMaxLength(64);

        builder.HasIndex(c => new { c.Name, c.Country }).IsUnique();
    }
}
