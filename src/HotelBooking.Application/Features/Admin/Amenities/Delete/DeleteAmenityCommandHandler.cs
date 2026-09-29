using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Amenities.Abstractions;
using HotelBooking.Application.Features.Admin.Amenities.Common;

namespace HotelBooking.Application.Features.Admin.Amenities.Delete;

public sealed class DeleteAmenityCommandHandler : ICommandHandler<DeleteAmenityCommand>
{
    private readonly IAmenityRepository _amenities;

    public DeleteAmenityCommandHandler(IAmenityRepository amenities) => _amenities = amenities;

    public async Task<Result> Handle(
        DeleteAmenityCommand command,
        CancellationToken cancellationToken)
    {
        var amenity = await _amenities.GetByIdAsync(command.Id, cancellationToken);

        if (amenity is null)
            return Result.Failure(AmenityErrors.NotFound(command.Id));

        _amenities.Remove(amenity);

        return Result.Success();
    }
}
