using HotelBooking.Application.Features.Admin.RoomTypes.Create;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Repositories;

namespace HotelBooking.Application.UnitTests.Features.Admin.RoomTypes.Create;

public class CreateRoomTypeCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Handle_HappyPath_AddsRoomTypeAndReturnsDto()
    {
        var sut = new CreateRoomTypeCommandHandler(new RoomTypeRepository(_db));

        var result = await sut.Handle(
            new CreateRoomTypeCommand("Suite", "Large suite"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Suite");
        result.Value.Description.Should().Be("Large suite");
        result.Value.NumberOfRooms.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenNameExistsWithDifferentCasing_ReturnsAlreadyExists()
    {
        _db.RoomTypes.Add(new RoomType { Name = "Suite" });
        _db.SaveChanges();

        var sut = new CreateRoomTypeCommandHandler(new RoomTypeRepository(_db));

        var result = await sut.Handle(
            new CreateRoomTypeCommand("SUITE", null),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoomType.AlreadyExists");
        result.Error.Message.Should().Contain("Suite");
    }
}
