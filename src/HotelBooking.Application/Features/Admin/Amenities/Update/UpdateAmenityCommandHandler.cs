using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Amenities.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Amenities.Update;

public sealed class UpdateAmenityCommandHandler
    : ICommandHandler<UpdateAmenityCommand, AmenityDto>
{
    private readonly IApplicationDbContext _db;

    public UpdateAmenityCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<AmenityDto>> Handle(
        UpdateAmenityCommand command,
        CancellationToken cancellationToken)
    {
        var amenity = await _db.Amenities
            .FirstOrDefaultAsync(a => a.Id == command.Id, cancellationToken);

        if (amenity is null)
            return AmenityErrors.NotFound(command.Id);

        if (command.Name is not null)
        {
            var newName = command.Name.Trim();
            if (!string.Equals(newName, amenity.Name, StringComparison.OrdinalIgnoreCase))
            {
                var normalized = newName.ToLower();
                var conflictName = await _db.Amenities
                    .Where(a => a.Id != command.Id && a.Name.ToLower() == normalized)
                    .Select(a => a.Name)
                    .FirstOrDefaultAsync(cancellationToken);

                if (conflictName is not null)
                    return AmenityErrors.AlreadyExists(conflictName);
            }

            amenity.Name = newName;
        }

        if (command.Icon is not null)
            amenity.Icon = string.IsNullOrWhiteSpace(command.Icon)
                ? null
                : command.Icon.Trim();

        return new AmenityDto(
            amenity.Id,
            amenity.Name,
            amenity.Icon,
            amenity.CreatedAt,
            amenity.UpdatedAt);
    }
}