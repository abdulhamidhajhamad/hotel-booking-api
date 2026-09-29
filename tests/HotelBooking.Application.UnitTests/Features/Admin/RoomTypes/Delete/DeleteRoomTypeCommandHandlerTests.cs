using HotelBooking.Application.Features.Admin.RoomTypes.Delete;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Repositories.RoomTypes;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.UnitTests.Features.Admin.RoomTypes.Delete;

public class DeleteRoomTypeCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Handle_WhenNotFound_ReturnsNotFound()
    {
        var sut = new DeleteRoomTypeCommandHandler(new RoomTypeRepository(_db));

        var result = await sut.Handle(
            new DeleteRoomTypeCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoomType.NotFound");
    }

    [Fact]
    public async Task Handle_WhenAnyRoomUsesIt_ReturnsInUseByRooms()
    {
        var cityId = Guid.NewGuid();
        _db.Cities.Add(new City { Id = cityId, Name = "X", Country = "JO", Timezone = "Asia/Amman" });

        var hotel = new Hotel
        {
            Id = Guid.NewGuid(),
            Name = "H",
            StarRating = 3,
            Address = "x",
            CityId = cityId,
        };
        _db.Hotels.Add(hotel);

        var roomType = new RoomType { Id = Guid.NewGuid(), Name = "Suite" };
        _db.RoomTypes.Add(roomType);

        _db.Rooms.Add(new Room
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
        });
        _db.SaveChanges();

        var sut = new DeleteRoomTypeCommandHandler(new RoomTypeRepository(_db));

        var result = await sut.Handle(
            new DeleteRoomTypeCommand(roomType.Id),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RoomType.InUseByRooms");
    }

    [Fact]
    public async Task Handle_WhenNoRoomsUseIt_SoftDeletes()
    {
        var roomType = new RoomType { Id = Guid.NewGuid(), Name = "Suite" };
        _db.RoomTypes.Add(roomType);
        _db.SaveChanges();

        var sut = new DeleteRoomTypeCommandHandler(new RoomTypeRepository(_db));

        var result = await sut.Handle(
            new DeleteRoomTypeCommand(roomType.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _db.SaveChangesAsync();

        var stillVisible = await _db.RoomTypes.AnyAsync(t => t.Id == roomType.Id);
        stillVisible.Should().BeFalse();
    }
}
