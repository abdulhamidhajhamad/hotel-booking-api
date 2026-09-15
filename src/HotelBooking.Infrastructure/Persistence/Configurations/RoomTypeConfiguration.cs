using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
{
    public void Configure(EntityTypeBuilder<RoomType> builder)
    {
        builder.Property(t => t.Name).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Description).HasMaxLength(500);

        builder.HasIndex(t => t.Name).IsUnique();
    }
}
