using HotelBooking.Application.Features.Cities.GetTrending;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Repositories.TrendingDestinations;
using Microsoft.Extensions.Caching.Memory;

namespace HotelBooking.Application.UnitTests.Features.Cities.GetTrending;

public class GetTrendingDestinationsQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();
    private readonly MemoryCache _cache = new(new MemoryCacheOptions());
    private readonly GetTrendingDestinationsQueryHandler _sut;

    public GetTrendingDestinationsQueryHandlerTests()
        => _sut = new GetTrendingDestinationsQueryHandler(new TrendingDestinationsReader(_db), _cache);

    public void Dispose()
    {
        _db.Dispose();
        _cache.Dispose();
    }

    [Fact]
    public async Task Handle_WhenNoBookings_ReturnsEmpty()
    {
        var result = await _sut.Handle(new GetTrendingDestinationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RanksCitiesByBookingCountDescending()
    {
        var amman = SeedCity("Amman");
        var aqaba = SeedCity("Aqaba");
        AddBooking(SeedHotel(amman));
        AddBooking(SeedHotel(amman));
        AddBooking(SeedHotel(aqaba));

        var result = await _sut.Handle(new GetTrendingDestinationsQuery(), CancellationToken.None);

        result.Value.Select(c => c.Name).Should().ContainInOrder("Amman", "Aqaba");
        result.Value.First().BookingCount.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ExcludesCancelledBookings()
    {
        var city = SeedCity();
        AddBooking(SeedHotel(city), BookingStatus.Cancelled);

        var result = await _sut.Handle(new GetTrendingDestinationsQuery(), CancellationToken.None);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_LimitsToRequestedCount()
    {
        for (var i = 0; i < 6; i++)
            AddBooking(SeedHotel(SeedCity($"City {i}")));

        var result = await _sut.Handle(new GetTrendingDestinationsQuery(Count: 5), CancellationToken.None);

        result.Value.Should().HaveCount(5);
    }

    [Fact]
    public async Task Handle_UsesPrimaryCityImageAsThumbnail()
    {
        var city = SeedCity();
        AddBooking(SeedHotel(city));
        AddCityImage(city, "https://img/secondary.jpg", isPrimary: false);
        AddCityImage(city, "https://img/primary.jpg", isPrimary: true);

        var result = await _sut.Handle(new GetTrendingDestinationsQuery(), CancellationToken.None);

        result.Value.Should().ContainSingle()
            .Which.ThumbnailUrl.Should().Be("https://img/primary.jpg");
    }

    private City SeedCity(string name = "Amman", string country = "Jordan")
    {
        var city = new City { Name = name, Country = country, Timezone = "Asia/Amman" };
        _db.Cities.Add(city);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return city;
    }

    private Hotel SeedHotel(City city)
    {
        var hotel = new Hotel
        {
            Name = "Hotel",
            Address = "Address",
            StarRating = 3,
            CityId = city.Id,
            Rooms =
            {
                new Room
                {
                    RoomType = new RoomType { Name = "Standard" },
                    Number = "101",
                    AdultsCapacity = 2,
                    ChildrenCapacity = 0,
                    PricePerNight = 100m,
                    IsActive = true,
                    RowVersion = Array.Empty<byte>()
                }
            }
        };
        _db.Hotels.Add(hotel);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return hotel;
    }

    private void AddBooking(Hotel hotel, BookingStatus status = BookingStatus.Confirmed)
    {
        var group = new BookingGroup
        {
            UserId = Guid.NewGuid(),
            ConfirmationNumber = Guid.NewGuid().ToString("N")[..10],
            TotalPrice = 100m,
            RowVersion = Array.Empty<byte>()
        };
        _db.BookingGroups.Add(group);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        _db.Bookings.Add(new Booking
        {
            BookingGroupId = group.Id,
            RoomId = hotel.Rooms.First().Id,
            CheckInDate = today,
            CheckOutDate = today.AddDays(1),
            AdultsCount = 2,
            ChildrenCount = 0,
            PricePerNightSnapshot = 100m,
            TotalPrice = 100m,
            Status = status,
            RowVersion = Array.Empty<byte>()
        });
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }

    private void AddCityImage(City city, string url, bool isPrimary)
    {
        _db.CityImages.Add(new CityImage
        {
            CityId = city.Id,
            Url = url,
            PublicId = "pid",
            IsPrimary = isPrimary
        });
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }
}