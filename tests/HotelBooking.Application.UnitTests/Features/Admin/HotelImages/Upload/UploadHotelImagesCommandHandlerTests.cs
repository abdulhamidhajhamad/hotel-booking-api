using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Common.Storage;
using HotelBooking.Application.Features.Admin.HotelImages.Upload;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Repositories.HotelImages;
using Microsoft.Extensions.Logging;

namespace HotelBooking.Application.UnitTests.Features.Admin.HotelImages.Upload;

public class UploadHotelImagesCommandHandlerTests
{
    private readonly IImageStorage _storage = Substitute.For<IImageStorage>();
    private readonly ILogger<UploadHotelImagesCommandHandler> _logger =
        Substitute.For<ILogger<UploadHotelImagesCommandHandler>>();

    private static UploadImageFile FakeFile() =>
        new(new MemoryStream(new byte[] { 1, 2, 3 }), "image/jpeg", "test.jpg");

    private static Hotel SeedHotel(ApplicationDbContext db)
    {
        var cityId = Guid.NewGuid();
        db.Cities.Add(new City { Id = cityId, Name = "X", Country = "JO", Timezone = "Asia/Amman" });
        var hotel = new Hotel
        {
            Id = Guid.NewGuid(),
            Name = "Test Hotel",
            StarRating = 3,
            Address = "somewhere",
            CityId = cityId,
        };
        db.Hotels.Add(hotel);
        db.SaveChanges();
        return hotel;
    }

    [Fact]
    public async Task Handle_WhenHotelNotFound_ReturnsValidationError()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new UploadHotelImagesCommandHandler(new HotelImageRepository(db), db, _storage, _logger);

        var result = await sut.Handle(
            new UploadHotelImagesCommand(Guid.NewGuid(), new[] { FakeFile() }),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("HotelImage.HotelNotFound");
    }

    [Fact]
    public async Task Handle_WhenHotelHasNoImages_MakesFirstFileInBatchPrimary()
    {
        await using var db = TestDbContextFactory.Create();
        var hotel = SeedHotel(db);

        _storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new StoredImage($"https://cdn/{Guid.NewGuid():N}", Guid.NewGuid().ToString("N")));

        var sut = new UploadHotelImagesCommandHandler(new HotelImageRepository(db), db, _storage, _logger);

        var result = await sut.Handle(
            new UploadHotelImagesCommand(hotel.Id, new[] { FakeFile(), FakeFile(), FakeFile() }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(3);
        result.Value[0].IsPrimary.Should().BeTrue();
        result.Value.Skip(1).Should().OnlyContain(i => !i.IsPrimary);
    }

    [Fact]
    public async Task Handle_WhenHotelAlreadyHasPrimary_AllNewFilesAreNonPrimary()
    {
        await using var db = TestDbContextFactory.Create();
        var hotel = SeedHotel(db);
        db.HotelImages.Add(new HotelImage
        {
            HotelId = hotel.Id,
            Url = "https://cdn/existing",
            PublicId = "existing",
            IsPrimary = true,
        });
        db.SaveChanges();

        _storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(_ => new StoredImage($"https://cdn/{Guid.NewGuid():N}", Guid.NewGuid().ToString("N")));

        var sut = new UploadHotelImagesCommandHandler(new HotelImageRepository(db), db, _storage, _logger);

        var result = await sut.Handle(
            new UploadHotelImagesCommand(hotel.Id, new[] { FakeFile(), FakeFile() }),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().OnlyContain(i => !i.IsPrimary);
    }

    [Fact]
    public async Task Handle_UploadsEachFileToHotelScopedFolder()
    {
        await using var db = TestDbContextFactory.Create();
        var hotel = SeedHotel(db);
        _storage.UploadAsync(Arg.Any<Stream>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new StoredImage("https://cdn/x", "px"));

        var sut = new UploadHotelImagesCommandHandler(new HotelImageRepository(db), db, _storage, _logger);

        await sut.Handle(
            new UploadHotelImagesCommand(hotel.Id, new[] { FakeFile(), FakeFile() }),
            CancellationToken.None);

        await _storage.Received(2).UploadAsync(
            Arg.Any<Stream>(),
            "image/jpeg",
            $"hotels/{hotel.Id}",
            Arg.Any<CancellationToken>());
    }
}