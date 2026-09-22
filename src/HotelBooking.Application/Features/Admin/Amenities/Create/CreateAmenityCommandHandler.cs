using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Amenities.Common;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Amenities.Create;

public sealed class CreateAmenityCommandHandler
    : ICommandHandler<CreateAmenityCommand, AmenityDto>
{
    private readonly IApplicationDbContext _db;

    public CreateAmenityCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<AmenityDto>> Handle(
        CreateAmenityCommand command,
        CancellationToken cancellationToken)
    {
        var name = command.Name.Trim();
        var normalized = name.ToLower();

        var existingName = await _db.Amenities
            .Where(a => a.Name.ToLower() == normalized)
            .Select(a => a.Name)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingName is not null)
            return AmenityErrors.AlreadyExists(existingName);

        var amenity = new Amenity
        {
            Name = name,
            Icon = string.IsNullOrWhiteSpace(command.Icon) ? null : command.Icon.Trim(),
        };

        await _db.Amenities.AddAsync(amenity, cancellationToken);

        return new AmenityDto(
            amenity.Id,
            amenity.Name,
            amenity.Icon,
            amenity.CreatedAt,
            amenity.UpdatedAt);
    }
}