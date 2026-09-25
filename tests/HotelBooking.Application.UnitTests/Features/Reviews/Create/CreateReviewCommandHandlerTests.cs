using HotelBooking.Application.Features.Reviews.Create;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.Extensions.Time.Testing;

namespace HotelBooking.Application.UnitTests.Features.Reviews.Create;

public class CreateReviewCommandHandlerTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 6, 1);

    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));
    private readonly Guid _userId = Guid.NewGuid();
    private readonly FakeCurrentUser _currentUser;

    public CreateReviewCommandHandlerTests() => _currentUser = FakeCurrentUser.SignedIn(_userId);

    public void Dispose() => _db.Dispose();

    private CreateReviewCommandHandler CreateSut() => new(_db, _currentUser, _time);

    private Booking SeedBooking(Guid ownerId, BookingStatus status, DateOnly checkOut)
    {
        var room = new Room
        {
            HotelId = Guid.NewGuid(),
            RoomTypeId = Guid.NewGuid(),
            Number = "101",
            AdultsCapacity = 2,
            ChildrenCapacity = 1,
            PricePerNight = 100m,
        };
        var group = new BookingGroup
        {
            UserId = ownerId,
            ConfirmationNumber = "HB-TEST",
            TotalPrice = 200m,
        };
        var booking = new Booking
        {
            BookingGroupId = group.Id,
            BookingGroup = group,
            RoomId = room.Id,
            Room = room,
            CheckInDate = checkOut.AddDays(-2),
            CheckOutDate = checkOut,
            AdultsCount = 2,
            ChildrenCount = 0,
            PricePerNightSnapshot = 100m,
            OriginalPricePerNightSnapshot = 100m,
            TotalPrice = 200m,
            Status = status,
        };
        _db.Rooms.Add(room);
        _db.BookingGroups.Add(group);
        _db.Bookings.Add(booking);
        _db.SaveChanges();
        return booking;
    }

    [Fact]
    public async Task Handle_WhenConfirmedAndStayFinished_CreatesReview()
    {
        var booking = SeedBooking(_userId, BookingStatus.Confirmed, Today.AddDays(-1));

        var result = await CreateSut().Handle(
            new CreateReviewCommand(booking.Id, 5, "Great stay"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Rating.Should().Be(5);
        _db.Reviews.Single().BookingId.Should().Be(booking.Id);
    }

    [Fact]
    public async Task Handle_WhenAnonymous_ReturnsUnauthorized()
    {
        var booking = SeedBooking(_userId, BookingStatus.Confirmed, Today.AddDays(-1));
        var sut = new CreateReviewCommandHandler(_db, FakeCurrentUser.Anonymous(), _time);

        var result = await sut.Handle(
            new CreateReviewCommand(booking.Id, 5, null), CancellationToken.None);

        result.Error.Code.Should().Be("Review.NotAuthenticated");
    }

    [Fact]
    public async Task Handle_WhenBookingMissing_ReturnsNotFound()
    {
        var result = await CreateSut().Handle(
            new CreateReviewCommand(Guid.NewGuid(), 5, null), CancellationToken.None);

        result.Error.Code.Should().Be("Review.BookingNotFound");
    }

    [Fact]
    public async Task Handle_WhenBookingBelongsToAnotherUser_ReturnsForbidden()
    {
        var booking = SeedBooking(Guid.NewGuid(), BookingStatus.Confirmed, Today.AddDays(-1));

        var result = await CreateSut().Handle(
            new CreateReviewCommand(booking.Id, 5, null), CancellationToken.None);

        result.Error.Code.Should().Be("Review.NotBookingOwner");
    }

    [Fact]
    public async Task Handle_WhenCheckOutInFuture_ReturnsConflict()
    {
        var booking = SeedBooking(_userId, BookingStatus.Confirmed, Today.AddDays(3));

        var result = await CreateSut().Handle(
            new CreateReviewCommand(booking.Id, 5, null), CancellationToken.None);

        result.Error.Code.Should().Be("Review.StayNotCompleted");
    }

    [Fact]
    public async Task Handle_WhenNotConfirmed_ReturnsConflict()
    {
        var booking = SeedBooking(_userId, BookingStatus.Pending, Today.AddDays(-1));

        var result = await CreateSut().Handle(
            new CreateReviewCommand(booking.Id, 5, null), CancellationToken.None);

        result.Error.Code.Should().Be("Review.StayNotCompleted");
    }

    [Fact]
    public async Task Handle_WhenBookingAlreadyReviewed_ReturnsConflict()
    {
        var booking = SeedBooking(_userId, BookingStatus.Confirmed, Today.AddDays(-1));
        var sut = CreateSut();
        await sut.Handle(new CreateReviewCommand(booking.Id, 4, null), CancellationToken.None);
        _db.SaveChanges();

        var second = await sut.Handle(
            new CreateReviewCommand(booking.Id, 5, null), CancellationToken.None);

        second.Error.Code.Should().Be("Review.AlreadyReviewed");
    }
}