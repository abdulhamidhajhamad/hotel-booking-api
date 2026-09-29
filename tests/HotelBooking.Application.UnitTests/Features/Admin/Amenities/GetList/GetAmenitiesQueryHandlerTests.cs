using HotelBooking.Application.Features.Admin.Amenities.GetList;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Repositories;

namespace HotelBooking.Application.UnitTests.Features.Admin.Amenities.GetList;

public class GetAmenitiesQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Handle_WhenEmpty_ReturnsEmptyList()
    {
        var sut = new GetAmenitiesQueryHandler(new AmenityReader(_db));

        var result = await sut.Handle(new GetAmenitiesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsAmenitiesSortedByName()
    {
        _db.Amenities.AddRange(
            new Amenity { Name = "Pool" },
            new Amenity { Name = "Gym" },
            new Amenity { Name = "Wifi" });
        _db.SaveChanges();

        var sut = new GetAmenitiesQueryHandler(new AmenityReader(_db));

        var result = await sut.Handle(new GetAmenitiesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(a => a.Name).Should().ContainInOrder("Gym", "Pool", "Wifi");
    }
}
