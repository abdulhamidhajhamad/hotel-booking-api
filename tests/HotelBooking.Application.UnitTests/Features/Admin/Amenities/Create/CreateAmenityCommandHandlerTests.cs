using HotelBooking.Application.Features.Admin.Amenities.Create;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Repositories;

namespace HotelBooking.Application.UnitTests.Features.Admin.Amenities.Create;

public class CreateAmenityCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Handle_HappyPath_AddsAmenityAndReturnsDto()
    {
        var sut = new CreateAmenityCommandHandler(new AmenityRepository(_db));

        var result = await sut.Handle(
            new CreateAmenityCommand("  Pool  ", "pool-icon"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Pool");
        result.Value.Icon.Should().Be("pool-icon");
    }

    [Fact]
    public async Task Handle_WhenNameExistsWithDifferentCasing_ReturnsAlreadyExists()
    {
        _db.Amenities.Add(new Amenity { Name = "Pool" });
        _db.SaveChanges();

        var sut = new CreateAmenityCommandHandler(new AmenityRepository(_db));

        var result = await sut.Handle(
            new CreateAmenityCommand("POOL", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Amenity.AlreadyExists");
        result.Error.Message.Should().Contain("Pool");
    }
}
