using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class HotelConfiguration : IEntityTypeConfiguration<Hotel>
{
    public void Configure(EntityTypeBuilder<Hotel> builder)
    {
        builder.Property(h => h.Name).IsRequired().HasMaxLength(200);
        builder.Property(h => h.Description).HasMaxLength(2000);
        builder.Property(h => h.Address).IsRequired().HasMaxLength(300);
        builder.Property(h => h.Category).HasConversion<string>().HasMaxLength(20);
        builder.Property(h => h.OwnerName).HasMaxLength(200);

        builder.HasOne(h => h.City)
            .WithMany(c => c.Hotels)
            .HasForeignKey(h => h.CityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Hotel_StarRating",
            "[StarRating] BETWEEN 1 AND 5"));
    }
}