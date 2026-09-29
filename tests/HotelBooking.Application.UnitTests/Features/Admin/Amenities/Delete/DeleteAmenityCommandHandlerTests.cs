using HotelBooking.Application.Features.Admin.Amenities.Delete;
using HotelBooking.Application.UnitTests.Common.Fakes;
using HotelBooking.Domain.Entities;
using HotelBooking.Infrastructure.Persistence;
using HotelBooking.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.UnitTests.Features.Admin.Amenities.Delete;

public class DeleteAmenityCommandHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _db = TestDbContextFactory.Create();

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task Handle_WhenAmenityNotFound_ReturnsNotFound()
    {
        var sut = new DeleteAmenityCommandHandler(new AmenityRepository(_db));

        var result = await sut.Handle(
            new DeleteAmenityCommand(Guid.NewGuid()),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Amenity.NotFound");
    }

    [Fact]
    public async Task Handle_HardDeletesAmenityRow()
    {
        var amenity = new Amenity { Id = Guid.NewGuid(), Name = "Pool" };
        _db.Amenities.Add(amenity);
        _db.SaveChanges();

        var sut = new DeleteAmenityCommandHandler(new AmenityRepository(_db));

        var result = await sut.Handle(
            new DeleteAmenityCommand(amenity.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _db.SaveChangesAsync();

        (await _db.Amenities.CountAsync()).Should().Be(0);
    }
}
