using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Amenities.Abstractions;
using HotelBooking.Application.Features.Admin.Amenities.Common;

namespace HotelBooking.Application.Features.Admin.Amenities.Update;

public sealed class UpdateAmenityCommandHandler
    : ICommandHandler<UpdateAmenityCommand, AmenityDto>
{
    private readonly IAmenityRepository _amenities;

    public UpdateAmenityCommandHandler(IAmenityRepository amenities) => _amenities = amenities;

    public async Task<Result<AmenityDto>> Handle(
        UpdateAmenityCommand command,
        CancellationToken cancellationToken)
    {
        var amenity = await _amenities.GetByIdAsync(command.Id, cancellationToken);

        if (amenity is null)
            return AmenityErrors.NotFound(command.Id);

        if (command.Name is not null)
        {
            var newName = command.Name.Trim();
            if (!string.Equals(newName, amenity.Name, StringComparison.OrdinalIgnoreCase))
            {
                var conflictName = await _amenities.GetExistingNameAsync(newName, command.Id, cancellationToken);

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
