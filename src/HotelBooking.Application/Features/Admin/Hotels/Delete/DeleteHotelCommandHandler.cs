using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Hotels.Common;
using HotelBooking.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Hotels.Delete;

public sealed class DeleteHotelCommandHandler : ICommandHandler<DeleteHotelCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteHotelCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(
        DeleteHotelCommand command,
        CancellationToken cancellationToken)
    {
        var hotel = await _db.Hotels
            .FirstOrDefaultAsync(h => h.Id == command.Id, cancellationToken);

        if (hotel is null)
            return Result.Failure(HotelErrors.NotFound(command.Id));

        var hasActive = await _db.Bookings.AnyAsync(
            b => b.Room.HotelId == command.Id
                 && (b.Status == BookingStatus.Pending
                     || b.Status == BookingStatus.Confirmed
                     || b.Status == BookingStatus.CheckedIn),
            cancellationToken);

        if (hasActive)
            return Result.Failure(HotelErrors.HasActiveBookings(command.Id));

        hotel.IsDeleted = true;
        hotel.DeletedAt = DateTimeOffset.UtcNow;

        return Result.Success();
    }
}