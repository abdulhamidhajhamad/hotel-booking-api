using HotelBooking.Application.Features.Hotels.Search;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;

namespace HotelBooking.Application.UnitTests.Features.Hotels.Search;

public class SearchHotelsQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();
    private readonly SearchHotelsQueryHandler _sut;

    public SearchHotelsQueryHandlerTests() => _sut = new SearchHotelsQueryHandler(_db);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Handle_WhenNoHotels_ReturnsEmpty()
    {
        var result = await _sut.Handle(new SearchHotelsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ExcludesHotel_WhenNoRoomFitsParty()
    {
        SeedHotel(name: "Fits", adults: 2, children: 1);
        SeedHotel(name: "TooSmall", adults: 1, children: 0);

        var result = await _sut.Handle(
            new SearchHotelsQuery(Adults: 2, Children: 1),
            CancellationToken.None);

        result.Value.Items.Select(h => h.HotelName)
            .Should().ContainSingle().Which.Should().Be("Fits");
    }

    [Fact]
    public async Task Handle_ExcludesHotel_WhenRoomBookedForRange()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hotel = SeedHotel();
        AddBooking(hotel, today, today.AddDays(2), BookingStatus.Confirmed);

        var result = await _sut.Handle(
            new SearchHotelsQuery(CheckIn: today, CheckOut: today.AddDays(1)),
            CancellationToken.None);

        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_IncludesHotel_WhenOverlappingBookingIsCancelled()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hotel = SeedHotel();
        AddBooking(hotel, today, today.AddDays(2), BookingStatus.Cancelled);

        var result = await _sut.Handle(
            new SearchHotelsQuery(CheckIn: today, CheckOut: today.AddDays(1)),
            CancellationToken.None);

        result.Value.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_IncludesHotel_WhenBookingEndsOnCheckInDay()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hotel = SeedHotel();
        AddBooking(hotel, today.AddDays(-2), today, BookingStatus.Confirmed);

        var result = await _sut.Handle(
            new SearchHotelsQuery(CheckIn: today, CheckOut: today.AddDays(1)),
            CancellationToken.None);

        result.Value.Items.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_ReturnsOriginalAndDiscountedPrice_WhenDiscountActive()
    {
        var hotel = SeedHotel(price: 200m);
        AddDiscount(hotel, 25m, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(1));

        var result = await _sut.Handle(new SearchHotelsQuery(), CancellationToken.None);

        var item = result.Value.Items.Should().ContainSingle().Which;
        item.OriginalPricePerNight.Should().Be(200m);
        item.DiscountedPricePerNight.Should().Be(150m);
    }

    [Fact]
    public async Task Handle_IgnoresExpiredDiscount()
    {
        var hotel = SeedHotel(price: 200m);
        AddDiscount(hotel, 25m, DateTime.UtcNow.AddDays(-5), DateTime.UtcNow.AddDays(-1));

        var result = await _sut.Handle(new SearchHotelsQuery(), CancellationToken.None);

        var item = result.Value.Items.Should().ContainSingle().Which;
        item.DiscountedPricePerNight.Should().Be(200m);
    }

    [Fact]
    public async Task Handle_ExcludesHotel_WhenMissingRequiredAmenity()
    {
        var hotel = SeedHotel();
        var wifi = new Amenity { Name = "Wifi" };
        var pool = new Amenity { Name = "Pool" };
        _db.Amenities.AddRange(wifi, pool);
        _db.HotelAmenities.Add(new HotelAmenity { HotelId = hotel.Id, AmenityId = wifi.Id });
        _db.SaveChanges();
        _db.ChangeTracker.Clear();

        var result = await _sut.Handle(
            new SearchHotelsQuery(AmenityIds: new List<Guid> { wifi.Id, pool.Id }),
            CancellationToken.None);

        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_SortsByDiscountedPriceAscending_AndPaginates()
    {
        SeedHotel(name: "C", price: 300m);
        SeedHotel(name: "A", price: 100m);
        SeedHotel(name: "B", price: 200m);

        var result = await _sut.Handle(
            new SearchHotelsQuery(SortBy: "price", PageSize: 2, Page: 1),
            CancellationToken.None);

        result.Value.TotalCount.Should().Be(3);
        result.Value.Items.Select(h => h.DiscountedPricePerNight)
            .Should().ContainInOrder(100m, 200m);
    }

    private Hotel SeedHotel(
        string name = "Hotel",
        int starRating = 3,
        HotelCategory category = HotelCategory.Standard,
        int adults = 2,
        int children = 0,
        decimal price = 100m,
        bool roomActive = true)
    {
        var room = new Room
        {
            RoomType = new RoomType { Name = "Standard" },
            Number = "101",
            AdultsCapacity = adults,
            ChildrenCapacity = children,
            PricePerNight = price,
            IsActive = roomActive,
            RowVersion = Array.Empty<byte>()
        };

        var hotel = new Hotel
        {
            Name = name,
            Address = "Address",
            StarRating = starRating,
            Category = category,
            City = new City { Name = "Amman", Country = "Jordan", Timezone = "Asia/Amman" },
            Rooms = { room }
        };

        _db.Hotels.Add(hotel);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return hotel;
    }

    private void AddBooking(Hotel hotel, DateOnly checkIn, DateOnly checkOut, BookingStatus status)
    {
        _db.Bookings.Add(new Booking
        {
            RoomId = hotel.Rooms.First().Id,
            BookingGroupId = Guid.NewGuid(),
            CheckInDate = checkIn,
            CheckOutDate = checkOut,
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

    private void AddDiscount(Hotel hotel, decimal percentage, DateTime startUtc, DateTime endUtc)
    {
        _db.Discounts.Add(new Discount
        {
            RoomId = hotel.Rooms.First().Id,
            Percentage = percentage,
            StartUtc = startUtc,
            EndUtc = endUtc
        });
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }
}