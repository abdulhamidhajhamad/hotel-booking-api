using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.UnitTests.Common.Fakes;

public sealed class TestApplicationDbContext : ApplicationDbContext
{
    public TestApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        StampRowVersions();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampRowVersions();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampRowVersions()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added)
                continue;

            if (entry.Metadata.FindProperty("RowVersion") is null)
                continue;

            var member = entry.Property("RowVersion");
            member.CurrentValue ??= new byte[8];
        }
    }
}