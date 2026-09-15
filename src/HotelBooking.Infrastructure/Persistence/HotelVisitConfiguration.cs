using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Infrastructure.Persistence.Configurations;

public class HotelVisitConfiguration : IEntityTypeConfiguration<HotelVisit>
{
    public void Configure(EntityTypeBuilder<HotelVisit> builder)
    {
        builder.HasKey(v => v.Id);

        builder.HasOne(v => v.User)
            .WithMany(u => u.HotelVisits)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Hotel)
            .WithMany(h => h.Visits)
            .HasForeignKey(v => v.HotelId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
