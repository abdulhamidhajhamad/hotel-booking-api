using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Hotels.Abstractions;
using HotelBooking.Application.Features.Admin.Hotels.Common;

namespace HotelBooking.Application.Features.Admin.Hotels.Delete;

public sealed class DeleteHotelCommandHandler : ICommandHandler<DeleteHotelCommand>
{
    private readonly IHotelRepository _hotels;

    public DeleteHotelCommandHandler(IHotelRepository hotels) => _hotels = hotels;

    public async Task<Result> Handle(
        DeleteHotelCommand command,
        CancellationToken cancellationToken)
    {
        var hotel = await _hotels.GetByIdAsync(command.Id, cancellationToken);

        if (hotel is null)
            return Result.Failure(HotelErrors.NotFound(command.Id));

        var hasActive = await _hotels.HasActiveBookingsAsync(command.Id, cancellationToken);

        if (hasActive)
            return Result.Failure(HotelErrors.HasActiveBookings(command.Id));

        hotel.IsDeleted = true;
        hotel.DeletedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }
}
