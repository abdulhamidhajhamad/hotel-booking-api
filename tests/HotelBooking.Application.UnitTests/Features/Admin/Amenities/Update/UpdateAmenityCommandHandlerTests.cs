using HotelBooking.Application.Features.Admin.Amenities.Update;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Repositories;

namespace HotelBooking.Application.UnitTests.Features.Admin.Amenities.Update;

public class UpdateAmenityCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Handle_WhenAmenityNotFound_ReturnsNotFound()
    {
        var sut = new UpdateAmenityCommandHandler(new AmenityRepository(_db));

        var result = await sut.Handle(
            new UpdateAmenityCommand(Guid.NewGuid(), Name: "Anything"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Amenity.NotFound");
    }

    [Fact]
    public async Task Handle_WhenNameCollidesWithAnotherAmenity_ReturnsAlreadyExists()
    {
        _db.Amenities.Add(new Amenity { Id = Guid.NewGuid(), Name = "Pool" });
        var target = new Amenity { Id = Guid.NewGuid(), Name = "Gym" };
        _db.Amenities.Add(target);
        _db.SaveChanges();

        var sut = new UpdateAmenityCommandHandler(new AmenityRepository(_db));

        var result = await sut.Handle(
            new UpdateAmenityCommand(target.Id, Name: "pool"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Amenity.AlreadyExists");
        result.Error.Message.Should().Contain("Pool");
    }

    [Fact]
    public async Task Handle_WhenOnlyCasingChangesOnOwnRow_UpdatesName()
    {
        var amenity = new Amenity { Id = Guid.NewGuid(), Name = "Pool" };
        _db.Amenities.Add(amenity);
        _db.SaveChanges();

        var sut = new UpdateAmenityCommandHandler(new AmenityRepository(_db));

        var result = await sut.Handle(
            new UpdateAmenityCommand(amenity.Id, Name: "POOL"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("POOL");
    }
}
