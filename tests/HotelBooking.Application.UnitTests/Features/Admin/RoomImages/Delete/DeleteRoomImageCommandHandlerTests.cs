using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Features.Admin.RoomImages.Delete;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Repositories.RoomImages;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotelBooking.Application.UnitTests.Features.Admin.RoomImages.Delete;

public class DeleteRoomImageCommandHandlerTests
{
    private readonly IImageStorage _storage = Substitute.For<IImageStorage>();

    private DeleteRoomImageCommandHandler CreateSut(ApplicationDbContext db)
        => new(new RoomImageRepository(db), _storage, NullLogger<DeleteRoomImageCommandHandler>.Instance);

    private static (Room Room, RoomImage Image) SeedRoomWithImage(ApplicationDbContext db)
    {
        var cityId = Guid.NewGuid();
        db.Cities.Add(new City { Id = cityId, Name = "X", Country = "JO", Timezone = "Asia/Amman" });

        var hotel = new Hotel
        {
            Id = Guid.NewGuid(),
            Name = "H",
            StarRating = 3,
            Address = "x",
            CityId = cityId,
        };
        db.Hotels.Add(hotel);

        var roomType = new RoomType { Id = Guid.NewGuid(), Name = "Standard" };
        db.RoomTypes.Add(roomType);

        var room = new Room
        {
            Id = Guid.NewGuid(),
            HotelId = hotel.Id,
            RoomTypeId = roomType.Id,
            Number = "101",
            AdultsCapacity = 2,
            ChildrenCapacity = 0,
            PricePerNight = 100m,
            IsActive = true,
            RowVersion = Array.Empty<byte>(),
        };
        db.Rooms.Add(room);

        var image = new RoomImage { Id = Guid.NewGuid(), RoomId = room.Id, Url = "u", PublicId = "pub-1" };
        db.RoomImages.Add(image);

        db.SaveChanges();
        return (room, image);
    }

    [Fact]
    public async Task Handle_WhenImageNotFound_ReturnsNotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);

        var result = await sut.Handle(
            new DeleteRoomImageCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoomImage.NotFound");
    }

    [Fact]
    public async Task Handle_CallsStorageDelete_WithImagePublicId()
    {
        await using var db = TestDbContextFactory.Create();
        var (room, image) = SeedRoomWithImage(db);
        var sut = CreateSut(db);

        var result = await sut.Handle(
            new DeleteRoomImageCommand(room.HotelId, room.Id, image.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _storage.Received(1).DeleteAsync("pub-1", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStorageDeleteThrows_StillReturnsSuccess()
    {
        await using var db = TestDbContextFactory.Create();
        var (room, image) = SeedRoomWithImage(db);
        _storage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("boom")));

        var sut = CreateSut(db);

        var result = await sut.Handle(
            new DeleteRoomImageCommand(room.HotelId, room.Id, image.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}