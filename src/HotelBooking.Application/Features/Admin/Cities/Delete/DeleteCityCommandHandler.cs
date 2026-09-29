using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Cities.Abstractions;
using HotelBooking.Application.Features.Admin.Cities.Common;

namespace HotelBooking.Application.Features.Admin.Cities.Delete;

public sealed class DeleteCityCommandHandler : ICommandHandler<DeleteCityCommand>
{
    private readonly ICityRepository _cities;

    public DeleteCityCommandHandler(ICityRepository cities) => _cities = cities;

    public async Task<Result> Handle(
        DeleteCityCommand command,
        CancellationToken cancellationToken)
    {
        var city = await _cities.GetByIdAsync(command.Id, cancellationToken);

        if (city is null)
            return Result.Failure(CityErrors.NotFound(command.Id));

        var hasHotels = await _cities.HasHotelsAsync(command.Id, cancellationToken);

        if (hasHotels)
            return Result.Failure(CityErrors.HasHotels(command.Id));

        city.IsDeleted = true;
        city.DeletedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }
}
