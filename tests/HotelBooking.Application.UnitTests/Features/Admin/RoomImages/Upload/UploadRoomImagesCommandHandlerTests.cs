using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Features.Admin.RoomImages.Upload;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.UnitTests.Features.Admin.RoomImages.Upload;

public class UploadRoomImagesCommandHandlerTests
{
    private readonly IImageStorage _storage = Substitute.For<IImageStorage>();
    private readonly ILogger<UploadRoomImagesCommandHandler> _logger =
        Substitute.For<ILogger<UploadRoomImagesCommandHandler>>();

    private static UploadImageFile FakeFile() =>
        new(new MemoryStream(new byte[] { 1, 2, 3 }), "image/jpeg", "r.jpg");

    private static Room SeedRoom(ApplicationDbContext db)
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
        db.SaveChanges();
        return room;
    }

    [Fact]
    public async Task Handle_WhenRoomNotFoundUnderHotel_ReturnsValidationError()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new UploadRoomImagesCommandHandler(db, _storage, _logger);

        var result = await sut.Handle(
            new UploadRoomImagesCommand(Guid.NewGuid(), Guid.NewGuid(), new[] { FakeFile() }),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoomImage.RoomNotFound");
    }

    [Fact]
    public async Task Handle_UploadsEachFileAndReturnsDtos()
    {
        await using var db = TestDbContextFactory.Create();
        var room = SeedRoom(db);

        _storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new StoredImage($"https://cdn/{Guid.NewGuid():N}", Guid.NewGuid().ToString("N")));

        var sut = new UploadRoomImagesCommandHandler(db, _storage, _logger);

        var result = await sut.Handle(
            new UploadRoomImagesCommand(room.HotelId, room.Id, new[] { FakeFile(), FakeFile() }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_UploadsToRoomScopedFolder()
    {
        await using var db = TestDbContextFactory.Create();
        var room = SeedRoom(db);
        _storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StoredImage("u", "p"));

        var sut = new UploadRoomImagesCommandHandler(db, _storage, _logger);

        await sut.Handle(
            new UploadRoomImagesCommand(room.HotelId, room.Id, new[] { FakeFile() }),
            CancellationToken.None);

        await _storage.Received(1).UploadAsync(
            Arg.Any<Stream>(),
            "image/jpeg",
            $"hotels/{room.HotelId}/rooms/{room.Id}",
            Arg.Any<CancellationToken>());
    }
}