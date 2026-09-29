using HotelBooking.Application.Abstractions.Storage;
using HotelBooking.Application.Features.Admin.HotelImages.Delete;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Repositories.HotelImages;
using Microsoft.Extensions.Logging.Abstractions;

namespace HotelBooking.Application.UnitTests.Features.Admin.HotelImages.Delete;

public class DeleteHotelImageCommandHandlerTests
{
    private readonly IImageStorage _storage = Substitute.For<IImageStorage>();

    private DeleteHotelImageCommandHandler CreateSut(ApplicationDbContext db)
        => new(new HotelImageRepository(db), _storage, NullLogger<DeleteHotelImageCommandHandler>.Instance);

    private static Hotel SeedHotel(ApplicationDbContext db)
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
        db.SaveChanges();
        return hotel;
    }

    [Fact]
    public async Task Handle_WhenImageNotFound_ReturnsNotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = CreateSut(db);

        var result = await sut.Handle(
            new DeleteHotelImageCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("HotelImage.NotFound");
    }

    [Fact]
    public async Task Handle_WhenDeletingPrimaryAndOthersExist_PromotesOldestRemaining()
    {
        await using var db = TestDbContextFactory.Create();
        var hotel = SeedHotel(db);

        var primary = new HotelImage { HotelId = hotel.Id, Url = "u1", PublicId = "p1", IsPrimary = true, CreatedAt = DateTimeOffset.UtcNow.AddDays(-3) };
        var older   = new HotelImage { HotelId = hotel.Id, Url = "u2", PublicId = "p2", IsPrimary = false, CreatedAt = DateTimeOffset.UtcNow.AddDays(-2) };
        var newer   = new HotelImage { HotelId = hotel.Id, Url = "u3", PublicId = "p3", IsPrimary = false, CreatedAt = DateTimeOffset.UtcNow.AddDays(-1) };
        db.HotelImages.AddRange(primary, older, newer);
        db.SaveChanges();

        var sut = CreateSut(db);

        var result = await sut.Handle(
            new DeleteHotelImageCommand(hotel.Id, primary.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await db.SaveChangesAsync();

        (await db.HotelImages.FindAsync(older.Id))!.IsPrimary.Should().BeTrue();
        (await db.HotelImages.FindAsync(newer.Id))!.IsPrimary.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_CallsStorageDelete_WithImagePublicId()
    {
        await using var db = TestDbContextFactory.Create();
        var hotel = SeedHotel(db);
        var image = new HotelImage { HotelId = hotel.Id, Url = "u", PublicId = "public-xyz", IsPrimary = false };
        db.HotelImages.Add(image);
        db.SaveChanges();

        var sut = CreateSut(db);

        await sut.Handle(new DeleteHotelImageCommand(hotel.Id, image.Id), CancellationToken.None);

        await _storage.Received(1).DeleteAsync("public-xyz", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenStorageDeleteThrows_StillReturnsSuccess()
    {
        await using var db = TestDbContextFactory.Create();
        var hotel = SeedHotel(db);
        var image = new HotelImage { HotelId = hotel.Id, Url = "u", PublicId = "p", IsPrimary = false };
        db.HotelImages.Add(image);
        db.SaveChanges();

        _storage.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("cloudinary down")));

        var sut = CreateSut(db);

        var result = await sut.Handle(new DeleteHotelImageCommand(hotel.Id, image.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}