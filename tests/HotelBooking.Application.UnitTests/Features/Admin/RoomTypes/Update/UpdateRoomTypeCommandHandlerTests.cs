using HotelBooking.Application.Features.Admin.RoomTypes.Update;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Repositories;

namespace HotelBooking.Application.UnitTests.Features.Admin.RoomTypes.Update;

public class UpdateRoomTypeCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        var sut = new UpdateRoomTypeCommandHandler(new RoomTypeRepository(_db));

        var result = await sut.Handle(
            new UpdateRoomTypeCommand(Guid.NewGuid(), Name: "X"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoomType.NotFound");
    }

    [Fact]
    public async Task Handle_WhenNameCollidesWithAnother_ReturnsAlreadyExists()
    {
        _db.RoomTypes.Add(new RoomType { Id = Guid.NewGuid(), Name = "Suite" });
        var target = new RoomType { Id = Guid.NewGuid(), Name = "Standard" };
        _db.RoomTypes.Add(target);
        _db.SaveChanges();

        var sut = new UpdateRoomTypeCommandHandler(new RoomTypeRepository(_db));

        var result = await sut.Handle(
            new UpdateRoomTypeCommand(target.Id, Name: "suite"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoomType.AlreadyExists");
    }
}
