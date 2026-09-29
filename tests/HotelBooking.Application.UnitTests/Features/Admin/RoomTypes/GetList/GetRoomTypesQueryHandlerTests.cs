using HotelBooking.Application.Features.Admin.RoomTypes.GetList;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Repositories;

namespace HotelBooking.Application.UnitTests.Features.Admin.RoomTypes.GetList;

public class GetRoomTypesQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Handle_WhenEmpty_ReturnsEmptyList()
    {
        var sut = new GetRoomTypesQueryHandler(new RoomTypeReader(_db));

        var result = await sut.Handle(new GetRoomTypesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsRoomTypesSortedByName()
    {
        _db.RoomTypes.AddRange(
            new RoomType { Name = "Suite" },
            new RoomType { Name = "Deluxe" },
            new RoomType { Name = "Standard" });
        _db.SaveChanges();

        var sut = new GetRoomTypesQueryHandler(new RoomTypeReader(_db));

        var result = await sut.Handle(new GetRoomTypesQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(t => t.Name).Should().ContainInOrder("Deluxe", "Standard", "Suite");
    }
}
