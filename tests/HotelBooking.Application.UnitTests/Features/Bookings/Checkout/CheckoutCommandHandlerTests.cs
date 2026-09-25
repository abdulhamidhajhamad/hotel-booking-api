using HotelBooking.Application.Features.Bookings.Checkout;
using HotelBooking.Application.Features.Bookings.Common;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;

namespace HotelBooking.Application.UnitTests.Features.Bookings.Checkout;

public class CheckoutCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();
    private readonly FakePaymentGateway _gateway = new();
    private readonly FakeOutbox _outbox = new();
    private readonly FakeCurrentUser _currentUser = FakeCurrentUser.SignedIn(Guid.NewGuid());

    public void Dispose() => _db.Dispose();

    private CheckoutCommandHandler CreateSut() => new(_db, _gateway, _currentUser, _outbox);

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

    private static CheckoutCommand CommandFor(Guid roomId, string key = "key-1", string? special = null) =>
        new(
            IdempotencyKey: key,
            PaymentMethodId: "pm_card_visa",
            SpecialRequests: special,
            Rooms: new[]
            {
                new CheckoutRoomItem(roomId, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 3), 2, 0)
            });

    [Fact]
    public async Task Handle_HappyPath_ConfirmsBookingAndEnqueuesEvent()
    {
        var roomId = SeedRoom();
        var sut = CreateSut();

        var result = await sut.Handle(CommandFor(roomId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.PaymentStatus.Should().Be("Succeeded");
        result.Value.TotalPrice.Should().Be(200m);

        _db.Bookings.Single().Status.Should().Be(BookingStatus.Confirmed);
        _db.Payments.Single().Status.Should().Be(PaymentStatus.Succeeded);
        _db.RoomAvailability.Count().Should().Be(2);
        _outbox.Events.Should().ContainSingle(e => e is BookingConfirmedEvent);
        _gateway.ConfirmCallCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenPaymentDeclined_CancelsBookingAndReleasesHolds()
    {
        var roomId = SeedRoom();
        _gateway.ConfirmSucceeds = false;
        var sut = CreateSut();

        var result = await sut.Handle(CommandFor(roomId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Booking.PaymentFailed");

        _db.Bookings.Single().Status.Should().Be(BookingStatus.Cancelled);
        _db.Payments.Single().Status.Should().Be(PaymentStatus.Failed);
        _db.RoomAvailability.Should().BeEmpty();
        _outbox.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenSameKeyReplayed_ReturnsSameResultWithoutChargingAgain()
    {
        var roomId = SeedRoom();
        var sut = CreateSut();
        var command = CommandFor(roomId);

        var first = await sut.Handle(command, CancellationToken.None);
        var second = await sut.Handle(command, CancellationToken.None);

        first.IsSuccess.Should().BeTrue();
        second.IsSuccess.Should().BeTrue();
        second.Value.BookingGroupId.Should().Be(first.Value.BookingGroupId);
        _gateway.ConfirmCallCount.Should().Be(1);
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
        var sut = new CheckoutCommandHandler(_db, _gateway, FakeCurrentUser.Anonymous(), _outbox);

        var result = await sut.Handle(CommandFor(roomId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Booking.NotAuthenticated");
    }
}