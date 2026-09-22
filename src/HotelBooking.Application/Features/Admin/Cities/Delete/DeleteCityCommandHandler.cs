using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Cities.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Cities.Delete;

public sealed class DeleteCityCommandHandler : ICommandHandler<DeleteCityCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteCityCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(
        DeleteCityCommand command,
        CancellationToken cancellationToken)
    {
        var city = await _db.Cities
            .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

        if (city is null)
            return Result.Failure(CityErrors.NotFound(command.Id));

        var hasHotels = await _db.Hotels
            .AnyAsync(h => h.CityId == command.Id, cancellationToken);

        if (hasHotels)
            return Result.Failure(CityErrors.HasHotels(command.Id));

        city.IsDeleted = true;
        city.DeletedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }
}