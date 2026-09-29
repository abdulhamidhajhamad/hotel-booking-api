using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Amenities.Abstractions;
using HotelBooking.Application.Features.Admin.Amenities.Common;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Admin.Amenities.Create;

public sealed class CreateAmenityCommandHandler
    : ICommandHandler<CreateAmenityCommand, AmenityDto>
{
    private readonly IAmenityRepository _amenities;

    public CreateAmenityCommandHandler(IAmenityRepository amenities) => _amenities = amenities;

    public async Task<Result<AmenityDto>> Handle(
        CreateAmenityCommand command,
        CancellationToken cancellationToken)
    {
        var name = command.Name.Trim();

        var existingName = await _amenities.GetExistingNameAsync(name, null, cancellationToken);

        if (existingName is not null)
            return AmenityErrors.AlreadyExists(existingName);

        var amenity = new Amenity
        {
            Name = name,
            Icon = string.IsNullOrWhiteSpace(command.Icon) ? null : command.Icon.Trim(),
        };

        _amenities.Add(amenity);

        return new AmenityDto(
            amenity.Id,
            amenity.Name,
            amenity.Icon,
            amenity.CreatedAt,
            amenity.UpdatedAt);
    }
}
