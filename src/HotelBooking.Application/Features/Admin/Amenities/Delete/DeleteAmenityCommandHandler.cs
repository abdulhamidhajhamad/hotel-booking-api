using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Amenities.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Amenities.Delete;

public sealed class DeleteAmenityCommandHandler : ICommandHandler<DeleteAmenityCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteAmenityCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(
        DeleteAmenityCommand command,
        CancellationToken cancellationToken)
    {
        var amenity = await _db.Amenities
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (amenity is null)
            return Result.Failure(AmenityErrors.NotFound(command.Id));

        _db.Amenities.Remove(amenity);

        return Result.Success();
    }
}