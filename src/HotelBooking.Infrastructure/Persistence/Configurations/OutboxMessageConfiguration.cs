using HotelBooking.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Type)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Payload)
            .IsRequired();

        builder.Property(m => m.OccurredAtUtc).IsRequired();
        builder.Property(m => m.NextAttemptAtUtc).IsRequired();
        builder.Property(m => m.AttemptCount).IsRequired();

        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<byte>();

        builder.Property(m => m.LastError);

        builder.HasIndex(m => new { m.Status, m.NextAttemptAtUtc })
            .HasDatabaseName("IX_OutboxMessages_Status_NextAttemptAtUtc")
            .HasFilter("[Status] = 0");
    }
}