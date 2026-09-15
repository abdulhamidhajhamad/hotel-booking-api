using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Currency).IsRequired().HasMaxLength(3).IsFixedLength();
        builder.Property(p => p.Provider).IsRequired().HasMaxLength(50);
        builder.Property(p => p.ProviderTransactionId).HasMaxLength(200);
        builder.Property(p => p.IdempotencyKey).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(p => p.BookingGroup)
            .WithMany(g => g.Payments)
            .HasForeignKey(p => p.BookingGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.IdempotencyKey).IsUnique();
        builder.HasIndex(p => p.ProviderTransactionId)
            .IsUnique()
            .HasFilter("[ProviderTransactionId] IS NOT NULL");

        builder.ToTable(t => t.HasCheckConstraint(
            "CK_Payment_Amount",
            "[Amount] > 0"));
    }
}
