using HotelBooking.Application.Features.Bookings.Create;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Repositories.CreateBooking;

namespace HotelBooking.Application.UnitTests.Features.Bookings.Create;

public class CreateBookingCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();
    private readonly FakeCurrentUser _currentUser = FakeCurrentUser.SignedIn(Guid.NewGuid());

    public void Dispose() => _db.Dispose();

    private CreateBookingCommandHandler CreateSut() => new(new CreateBookingRepository(_db), _currentUser);

    private Guid SeedRoom(decimal pricePerNight = 100m, bool isActive = true)
    {
        var room = new Room
        {
            HotelId = Guid.NewGuid(),
            RoomTypeId = Guid.NewGuid(),
            Number = "101",
            AdultsCapacity = 2,
            ChildrenCapacity = 1,
            PricePerNight = pricePerNight,
            IsActive = isActive,
        };
        _db.Rooms.Add(room);
        _db.SaveChanges();
        return room.Id;
    }

    private static CreateBookingCommand CommandFor(Guid roomId, string key = "key-1", string? special = null) =>
        new(
            IdempotencyKey: key,
            SpecialRequests: special,
            Rooms: new[]
            {
                new CreateBookingRoomItem(roomId, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 3), 2, 0)
            });

    [Fact]
    public async Task Handle_HappyPath_CreatesPendingHold()
    {
        var roomId = SeedRoom();
        var sut = CreateSut();

        var result = await sut.Handle(CommandFor(roomId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalPrice.Should().Be(200m);

        _db.Bookings.Single().Status.Should().Be(BookingStatus.Pending);
        _db.RoomAvailability.Count().Should().Be(2);
        _db.Payments.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenSameKeyReplayed_ReturnsSameHold()
    {
        var roomId = SeedRoom();
        var sut = CreateSut();
        var command = CommandFor(roomId);

        var first = await sut.Handle(command, CancellationToken.None);
        var second = await sut.Handle(command, CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.Value.BookingGroupId.Should().Be(first.Value.BookingGroupId);
        _db.BookingGroups.Count().Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenSameKeyDifferentPayload_ReturnsIdempotencyKeyReused()
    {
        var roomId = SeedRoom();
        var sut = CreateSut();

        await sut.Handle(CommandFor(roomId, key: "key-9", special: "quiet room"), CancellationToken.None);
        var second = await sut.Handle(CommandFor(roomId, key: "key-9", special: "high floor"), CancellationToken.None);

        second.IsFailure.Should().BeTrue();
        second.Error.Code.Should().Be("Booking.IdempotencyKeyReused");
    }

    [Fact]
    public async Task Handle_WhenRoomAlreadyHeld_ReturnsRoomNotAvailable()
    {
        var roomId = SeedRoom();
        _db.RoomAvailability.Add(new RoomAvailability
        {
            RoomId = roomId,
            Date = new DateOnly(2026, 10, 1),
            BookingId = Guid.NewGuid(),
        });
        _db.SaveChanges();
        var sut = CreateSut();

        var result = await sut.Handle(CommandFor(roomId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Booking.RoomNotAvailable");
    }

    [Fact]
    public async Task Handle_WhenNotAuthenticated_ReturnsUnauthorized()
    {
        var roomId = SeedRoom();
        var sut = new CreateBookingCommandHandler(new CreateBookingRepository(_db), FakeCurrentUser.Anonymous());

        var result = await sut.Handle(CommandFor(roomId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Booking.NotAuthenticated");
    }
}