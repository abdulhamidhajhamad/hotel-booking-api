using System.Diagnostics;
using HotelBooking.Application.Features.Bookings.Common;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Entities;
using HotelBooking.Domain.Identity;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotelBooking.Api.IntegrationTests.Features.Bookings;

[Collection(IntegrationTestCollection.Name)]
public sealed class ExpiredHoldSweeperIntegrationTests
{
    private readonly HotelBookingApiFactory _factory;

    public ExpiredHoldSweeperIntegrationTests(HotelBookingApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Sweeper_CancelsExpiredPendingBooking_AndReleasesHolds()
    {
        Guid bookingId;
        var checkIn = new DateOnly(2027, 1, 10);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = $"guest-{Guid.NewGuid():N}@test.local",
                Email = $"guest-{Guid.NewGuid():N}@test.local",
                SecurityStamp = Guid.NewGuid().ToString("N"),
                ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            };
            user.NormalizedUserName = user.UserName!.ToUpperInvariant();
            user.NormalizedEmail = user.Email!.ToUpperInvariant();

            var city = new City
            {
                Name = $"City-{Guid.NewGuid():N}",
                Country = "TL",
                Timezone = "UTC",
            };

            var hotel = new Hotel
            {
                Name = $"Hotel-{Guid.NewGuid():N}",
                Address = "1 Test Street",
                StarRating = 4,
                City = city,
            };

            var roomType = new RoomType { Name = $"Type-{Guid.NewGuid():N}" };

            var room = new Room
            {
                Hotel = hotel,
                RoomType = roomType,
                Number = "101",
                AdultsCapacity = 2,
                ChildrenCapacity = 1,
                PricePerNight = 100m,
                IsActive = true,
            };

            var group = new BookingGroup
            {
                UserId = user.Id,
                ConfirmationNumber = $"HB-{Guid.NewGuid():N}"[..11].ToUpperInvariant(),
                TotalPrice = 100m,
            };

            var booking = new Booking
            {
                BookingGroup = group,
                Room = room,
                CheckInDate = checkIn,
                CheckOutDate = checkIn.AddDays(1),
                AdultsCount = 2,
                ChildrenCount = 0,
                OriginalPricePerNightSnapshot = 100m,
                PricePerNightSnapshot = 100m,
                TotalPrice = 100m,
                Status = BookingStatus.Pending,
            };
            booking.AvailabilityHolds.Add(new RoomAvailability { Room = room, Date = checkIn });

            db.Users.Add(user);
            db.Rooms.Add(room);
            db.BookingGroups.Add(group);
            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            bookingId = booking.Id;

            var past = DateTimeOffset.UtcNow.AddMinutes(-(BookingHold.TtlMinutes + 5));
            await db.Bookings
                .Where(b => b.Id == bookingId)
                .ExecuteUpdateAsync(s => s.SetProperty(b => b.CreatedAt, past));
        }

        var cancelled = await WaitUntilAsync(
            async () =>
            {
                using var scope = _factory.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var status = await db.Bookings
                    .AsNoTracking()
                    .Where(b => b.Id == bookingId)
                    .Select(b => (BookingStatus?)b.Status)
                    .FirstOrDefaultAsync();

                return status == BookingStatus.Cancelled ? "cancelled" : null;
            },
            TimeSpan.FromSeconds(15));

        cancelled.Should().Be("cancelled");

        using (var verifyScope = _factory.Services.CreateScope())
        {
            var db = verifyScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var remainingHolds = await db.RoomAvailability
                .AsNoTracking()
                .CountAsync(a => a.BookingId == bookingId);
            remainingHolds.Should().Be(0);
        }
    }

    private static async Task<T?> WaitUntilAsync<T>(Func<Task<T?>> probe, TimeSpan timeout)
        where T : class
    {
        var interval = TimeSpan.FromMilliseconds(200);
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout)
        {
            var value = await probe();
            if (value is not null) return value;
            await Task.Delay(interval);
        }
        return await probe();
    }
}