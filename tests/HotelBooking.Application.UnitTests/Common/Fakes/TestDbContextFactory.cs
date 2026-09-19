using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.UnitTests.Common.Fakes;

public static class TestDbContextFactory
{
    public static ApplicationDbContext Create()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"HotelBookingTests_{Guid.NewGuid():N}")
            .Options;

        return new ApplicationDbContext(options);
    }
}