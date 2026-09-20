using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
    public void Configure(EntityTypeBuilder<Discount> builder)
    {
        builder.Property(d => d.Title).HasMaxLength(200);
        builder.Property(d => d.Percentage).HasColumnType("decimal(5,2)");

        builder.HasOne(d => d.Room)
            .WithMany(r => r.Discounts)
            .HasForeignKey(d => d.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Discount_Percentage",
            "[Percentage] > 0 AND [Percentage] <= 100"));

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Discount_DateRange",
            "[EndUtc] > [StartUtc]"));
    }
}