using HotelBooking.Application.Features.Hotels.RecentlyVisited;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;

namespace HotelBooking.Application.UnitTests.Features.Hotels.RecentlyVisited;

public class GetRecentlyVisitedHotelsQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();
    private readonly FakeCurrentUser _currentUser = FakeCurrentUser.SignedIn(Guid.NewGuid());
    private readonly GetRecentlyVisitedHotelsQueryHandler _sut;

    public GetRecentlyVisitedHotelsQueryHandlerTests()
        => _sut = new GetRecentlyVisitedHotelsQueryHandler(_db, _currentUser);

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var sut = new GetRecentlyVisitedHotelsQueryHandler(_db, FakeCurrentUser.Anonymous());

        var result = await sut.Handle(new GetRecentlyVisitedHotelsQuery(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Unauthorized);
    }

    [Fact]
    public async Task Handle_WhenUserHasNoBookings_ReturnsEmpty()
    {
        var result = await _sut.Handle(new GetRecentlyVisitedHotelsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsOnlyCurrentUsersHotels()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var mine = SeedHotel(name: "Mine");
        var other = SeedHotel(name: "Other");
        AddBooking(_currentUser.Id!.Value, mine, DateTimeOffset.UtcNow, today);
        AddBooking(Guid.NewGuid(), other, DateTimeOffset.UtcNow, today);

        var result = await _sut.Handle(new GetRecentlyVisitedHotelsQuery(), CancellationToken.None);

        result.Value.Select(h => h.HotelName)
            .Should().ContainSingle().Which.Should().Be("Mine");
    }

    [Fact]
    public async Task Handle_DedupesSameHotel_KeepingLatestBooking()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hotel = SeedHotel();
        AddBooking(_currentUser.Id!.Value, hotel, DateTimeOffset.UtcNow.AddDays(-5), today);
        AddBooking(_currentUser.Id!.Value, hotel, DateTimeOffset.UtcNow, today);

        var result = await _sut.Handle(new GetRecentlyVisitedHotelsQuery(), CancellationToken.None);

        var item = result.Value.Should().ContainSingle().Which;
        item.LastBookedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Handle_OrdersByMostRecentlyBooked()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var older = SeedHotel(name: "Older");
        var newer = SeedHotel(name: "Newer");
        AddBooking(_currentUser.Id!.Value, older, DateTimeOffset.UtcNow.AddDays(-2), today);
        AddBooking(_currentUser.Id!.Value, newer, DateTimeOffset.UtcNow, today);

        var result = await _sut.Handle(new GetRecentlyVisitedHotelsQuery(), CancellationToken.None);

        result.Value.Select(h => h.HotelName)
            .Should().ContainInOrder("Newer", "Older");
    }

    [Fact]
    public async Task Handle_ExcludesCancelledBookings()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hotel = SeedHotel();
        AddBooking(_currentUser.Id!.Value, hotel, DateTimeOffset.UtcNow, today, BookingStatus.Cancelled);

        var result = await _sut.Handle(new GetRecentlyVisitedHotelsQuery(), CancellationToken.None);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_LimitsToRequestedCount()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        for (var i = 0; i < 6; i++)
            AddBooking(
                _currentUser.Id!.Value,
                SeedHotel(name: $"Hotel {i}"),
                DateTimeOffset.UtcNow.AddMinutes(-i),
                today);

        var result = await _sut.Handle(new GetRecentlyVisitedHotelsQuery(Count: 5), CancellationToken.None);

        result.Value.Should().HaveCount(5);
    }

    private Hotel SeedHotel(string name = "Hotel", int starRating = 3, decimal price = 100m)
    {
        var room = new Room
        {
            RoomType = new RoomType { Name = "Standard" },
            Number = "101",
            AdultsCapacity = 2,
            ChildrenCapacity = 0,
            PricePerNight = price,
            IsActive = true,
            RowVersion = Array.Empty<byte>()
        };

        var hotel = new Hotel
        {
            Name = name,
            Address = "Address",
            StarRating = starRating,
            City = new City { Name = "Amman", Country = "Jordan", Timezone = "Asia/Amman" },
            Rooms = { room }
        };

        _db.Hotels.Add(hotel);
        _db.SaveChanges();
        _db.ChangeTracker.Clear();
        return hotel;
    }

    private void AddBooking(
        Guid userId,
        Hotel hotel,
        DateTimeOffset createdAt,
        DateOnly checkIn,
        BookingStatus status = BookingStatus.Confirmed)
    {
        var group = new BookingGroup
        {
            UserId = userId,
            ConfirmationNumber = Guid.NewGuid().ToString("N")[..10],
            TotalPrice = 100m,
            RowVersion = Array.Empty<byte>()
        };
        _db.BookingGroups.Add(group);

        _db.Bookings.Add(new Booking
        {
            BookingGroupId = group.Id,
            RoomId = hotel.Rooms.First().Id,
            CheckInDate = checkIn,
            CheckOutDate = checkIn.AddDays(1),
            AdultsCount = 2,
            ChildrenCount = 0,
            PricePerNightSnapshot = 100m,
            TotalPrice = 100m,
            Status = status,
            CreatedAt = createdAt,
            RowVersion = Array.Empty<byte>()
        });

        _db.SaveChanges();
        _db.ChangeTracker.Clear();
    }
}