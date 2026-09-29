using HotelBooking.Application.Features.Bookings.Common;
using HotelBooking.Application.Features.Bookings.Create;
using HotelBooking.Application.Features.Bookings.Pay;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Repositories.CreateBooking;
using HotelBooking.Infrastructure.Persistence.Repositories.PayBooking;

namespace HotelBooking.Application.UnitTests.Features.Bookings.Pay;

public class PayBookingCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();
    private readonly FakePaymentGateway _gateway = new();
    private readonly FakeOutbox _outbox = new();
    private readonly FakeCurrentUser _currentUser = FakeCurrentUser.SignedIn(Guid.NewGuid());

    public void Dispose() => _db.Dispose();

    private PayBookingCommandHandler CreateSut() => new(new PayBookingRepository(_db), _db, _gateway, _currentUser, _outbox);

    private Guid SeedRoom(decimal pricePerNight = 100m)
    {
        var room = new Room
        {
            HotelId = Guid.NewGuid(),
            RoomTypeId = Guid.NewGuid(),
            Number = "101",
            AdultsCapacity = 2,
            ChildrenCapacity = 1,
            PricePerNight = pricePerNight,
            IsActive = true,
        };
        _db.Rooms.Add(room);
        _db.SaveChanges();
        return room.Id;
    }

    private async Task<Guid> SeedHoldAsync(Guid roomId)
    {
        var create = new CreateBookingCommandHandler(new CreateBookingRepository(_db), _currentUser);
        var result = await create.Handle(
            new CreateBookingCommand(
                IdempotencyKey: "hold-1",
                SpecialRequests: null,
                Rooms: new[]
                {
                    new CreateBookingRoomItem(roomId, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 3), 2, 0)
                }),
            CancellationToken.None);

        return result.Value.BookingGroupId;
    }

    [Fact]
    public async Task Handle_HappyPath_ConfirmsBookingAndEnqueuesEvent()
    {
        var roomId = SeedRoom();
        var groupId = await SeedHoldAsync(roomId);
        var sut = CreateSut();

        var result = await sut.Handle(new PayBookingCommand(groupId, "pm_card_visa"), CancellationToken.None);

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
        var groupId = await SeedHoldAsync(roomId);
        _gateway.ConfirmSucceeds = false;
        var sut = CreateSut();

        var result = await sut.Handle(new PayBookingCommand(groupId, "pm_card_visa"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Booking.PaymentFailed");

        _db.Bookings.Single().Status.Should().Be(BookingStatus.Cancelled);
        _db.Payments.Single().Status.Should().Be(PaymentStatus.Failed);
        _db.RoomAvailability.Should().BeEmpty();
        _outbox.Events.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenAlreadyPaid_ReturnsSuccessWithoutChargingAgain()
    {
        var roomId = SeedRoom();
        var groupId = await SeedHoldAsync(roomId);
        var sut = CreateSut();

        await sut.Handle(new PayBookingCommand(groupId, "pm_card_visa"), CancellationToken.None);
        var second = await sut.Handle(new PayBookingCommand(groupId, "pm_card_visa"), CancellationToken.None);

        second.IsSuccess.Should().BeTrue();
        _gateway.ConfirmCallCount.Should().Be(1);
        _db.Payments.Count().Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenBookingNotFound_ReturnsNotFound()
    {
        var sut = CreateSut();

        var result = await sut.Handle(new PayBookingCommand(Guid.NewGuid(), "pm_card_visa"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Booking.NotFound");
    }

    [Fact]
    public async Task Handle_WhenNotOwner_ReturnsForbidden()
    {
        var roomId = SeedRoom();
        var groupId = await SeedHoldAsync(roomId);
        var otherUserSut = new PayBookingCommandHandler(
            new PayBookingRepository(_db), _db, _gateway, FakeCurrentUser.SignedIn(Guid.NewGuid()), _outbox);

        var result = await otherUserSut.Handle(new PayBookingCommand(groupId, "pm_card_visa"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Booking.Forbidden");
    }
}