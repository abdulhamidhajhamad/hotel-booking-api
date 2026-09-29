using HotelBooking.Application.Features.Admin.HotelImages.SetPrimary;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Repositories.HotelImages;

namespace HotelBooking.Application.UnitTests.Features.Admin.HotelImages.SetPrimary;

public class SetPrimaryHotelImageCommandHandlerTests
{
    private static Hotel SeedHotelWithImages(ApplicationDbContext db, out HotelImage a, out HotelImage b)
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
        a = new HotelImage { HotelId = hotel.Id, Url = "u1", PublicId = "p1", IsPrimary = true };
        b = new HotelImage { HotelId = hotel.Id, Url = "u2", PublicId = "p2", IsPrimary = false };
        db.HotelImages.AddRange(a, b);
        db.SaveChanges();
        return hotel;
    }

    [Fact]
    public async Task Handle_WhenImageNotFound_ReturnsNotFound()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new SetPrimaryHotelImageCommandHandler(new HotelImageRepository(db));

        var result = await sut.Handle(
            new SetPrimaryHotelImageCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("HotelImage.NotFound");
    }

    [Fact]
    public async Task Handle_DemotesCurrentPrimary_AndPromotesTarget()
    {
        await using var db = TestDbContextFactory.Create();
        var hotel = SeedHotelWithImages(db, out var oldPrimary, out var target);
        var sut = new SetPrimaryHotelImageCommandHandler(new HotelImageRepository(db));

        var result = await sut.Handle(
            new SetPrimaryHotelImageCommand(hotel.Id, target.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await db.SaveChangesAsync();
        (await db.HotelImages.FindAsync(oldPrimary.Id))!.IsPrimary.Should().BeFalse();
        (await db.HotelImages.FindAsync(target.Id))!.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTargetIsAlreadyPrimary_IsNoOp()
    {
        await using var db = TestDbContextFactory.Create();
        var hotel = SeedHotelWithImages(db, out var primary, out _);
        var sut = new SetPrimaryHotelImageCommandHandler(new HotelImageRepository(db));

        var result = await sut.Handle(
            new SetPrimaryHotelImageCommand(hotel.Id, primary.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}