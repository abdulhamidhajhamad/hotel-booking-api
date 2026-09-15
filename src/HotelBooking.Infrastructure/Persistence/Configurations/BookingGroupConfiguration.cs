using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class BookingGroupConfiguration : IEntityTypeConfiguration<BookingGroup>
{
    public void Configure(EntityTypeBuilder<BookingGroup> builder)
    {
        builder.Property(g => g.ConfirmationNumber).IsRequired().HasMaxLength(20);
        builder.HasIndex(g => g.ConfirmationNumber).IsUnique();
        builder.Property(g => g.TotalPrice).HasColumnType("decimal(18,2)");
        builder.Property(g => g.SpecialRequests).HasMaxLength(1000);
        builder.Property(g => g.RowVersion).IsRowVersion();

        builder.HasOne(g => g.User)
            .WithMany(u => u.BookingGroups)
            .HasForeignKey(g => g.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_BookingGroup_TotalPrice",
            "[TotalPrice] >= 0"));
    }
}
