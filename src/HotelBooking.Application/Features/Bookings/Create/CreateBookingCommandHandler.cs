using System.Security.Cryptography;
using System.Text;
using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Bookings.Common;
using HotelBooking.Application.Features.Bookings.Create.Abstractions;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Bookings.Create;

public sealed class CreateBookingCommandHandler : ICommandHandler<CreateBookingCommand, CreateBookingResult>
{
    private readonly ICreateBookingRepository _bookings;
    private readonly ICurrentUser _currentUser;

    public CreateBookingCommandHandler(ICreateBookingRepository bookings, ICurrentUser currentUser)
    {
        _bookings = bookings;
        _currentUser = currentUser;
    }

    public async Task<Result<CreateBookingResult>> Handle(
        CreateBookingCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Result<CreateBookingResult>.Failure(BookingErrors.NotAuthenticated());

        var requestHash = ComputeRequestHash(command, userId);

        var existing = await _bookings.GetIdempotencyRecordAsync(command.IdempotencyKey, cancellationToken);

        if (existing is not null)
            return await ReplayAsync(existing, requestHash, cancellationToken);

        var roomIds = command.Rooms.Select(r => r.RoomId).Distinct().ToList();

        var pricingByRoom = await _bookings.GetRoomPricingAsync(roomIds, cancellationToken);

        foreach (var item in command.Rooms)
        {
            if (!pricingByRoom.TryGetValue(item.RoomId, out var pricing))
                return Result<CreateBookingResult>.Failure(BookingErrors.RoomNotFound(item.RoomId));
            if (!pricing.IsActive)
                return Result<CreateBookingResult>.Failure(BookingErrors.RoomInactive(item.RoomId));
            if (item.Adults > pricing.AdultsCapacity || item.Children > pricing.ChildrenCapacity)
                return Result<CreateBookingResult>.Failure(BookingErrors.RoomCapacityExceeded(item.RoomId));
        }

        var requestedSlots = new HashSet<(Guid RoomId, DateOnly Date)>();
        foreach (var item in command.Rooms)
            for (var date = item.CheckInDate; date < item.CheckOutDate; date = date.AddDays(1))
                requestedSlots.Add((item.RoomId, date));

        var takenSlots = (await _bookings.GetTakenSlotsAsync(roomIds, cancellationToken)).ToHashSet();

        if (requestedSlots.Any(takenSlots.Contains))
            return Result<CreateBookingResult>.Failure(BookingErrors.RoomNotAvailable());

        var group = new BookingGroup
        {
            UserId = userId,
            ConfirmationNumber = NewConfirmationNumber(),
            SpecialRequests = command.SpecialRequests,
        };

        decimal groupTotal = 0m;

        foreach (var item in command.Rooms)
        {
            var pricing = pricingByRoom[item.RoomId];
            var nights = item.CheckOutDate.DayNumber - item.CheckInDate.DayNumber;
            var percentage = pricing.DiscountPercentage ?? 0m;
            var discounted = Math.Round(pricing.PricePerNight * (1 - percentage / 100m), 2);
            var bookingTotal = discounted * nights;
            groupTotal += bookingTotal;

            var booking = new Booking
            {
                BookingGroupId = group.Id,
                RoomId = item.RoomId,
                CheckInDate = item.CheckInDate,
                CheckOutDate = item.CheckOutDate,
                AdultsCount = item.Adults,
                ChildrenCount = item.Children,
                OriginalPricePerNightSnapshot = pricing.PricePerNight,
                PricePerNightSnapshot = discounted,
                TotalPrice = bookingTotal,
                Status = BookingStatus.Pending,
            };

            for (var date = item.CheckInDate; date < item.CheckOutDate; date = date.AddDays(1))
                booking.AvailabilityHolds.Add(new RoomAvailability
                {
                    RoomId = item.RoomId,
                    Date = date,
                });

            group.Bookings.Add(booking);
        }

        group.TotalPrice = groupTotal;

        var record = new IdempotencyRecord
        {
            Key = command.IdempotencyKey,
            RequestHash = requestHash,
            Status = IdempotencyStatus.Completed,
            BookingGroupId = group.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            CompletedAt = DateTimeOffset.UtcNow,
        };

        var persisted = await _bookings.TryPersistBookingAsync(group, record, cancellationToken);

        if (!persisted)
        {
            var raced = await _bookings.GetIdempotencyRecordAsync(command.IdempotencyKey, cancellationToken);

            return raced is not null
                ? await ReplayAsync(raced, requestHash, cancellationToken)
                : Result<CreateBookingResult>.Failure(BookingErrors.RoomNotAvailable());
        }

        return Result<CreateBookingResult>.Success(new CreateBookingResult(
            group.Id,
            group.ConfirmationNumber,
            groupTotal,
            group.CreatedAt.AddMinutes(BookingHold.TtlMinutes)));
    }

    private async Task<Result<CreateBookingResult>> ReplayAsync(
        IdempotencyRecord record,
        string requestHash,
        CancellationToken cancellationToken)
    {
        if (record.RequestHash != requestHash)
            return Result<CreateBookingResult>.Failure(BookingErrors.IdempotencyKeyReused());

        if (record.Status != IdempotencyStatus.Completed || record.BookingGroupId is not { } bookingGroupId)
            return Result<CreateBookingResult>.Failure(BookingErrors.RequestInProgress());

        var snapshot = await _bookings.GetBookingGroupSnapshotAsync(bookingGroupId, cancellationToken);

        if (snapshot is null)
            return Result<CreateBookingResult>.Failure(BookingErrors.RequestInProgress());

        return Result<CreateBookingResult>.Success(new CreateBookingResult(
            snapshot.Id,
            snapshot.ConfirmationNumber,
            snapshot.TotalPrice,
            snapshot.CreatedAt.AddMinutes(BookingHold.TtlMinutes)));
    }

    private static string NewConfirmationNumber() =>
        $"HB-{Guid.NewGuid():N}"[..11].ToUpperInvariant();

    private static string ComputeRequestHash(CreateBookingCommand command, Guid userId)
    {
        var builder = new StringBuilder();
        builder.Append(userId).Append('|')
               .Append(command.SpecialRequests);

        foreach (var room in command.Rooms.OrderBy(r => r.RoomId).ThenBy(r => r.CheckInDate.DayNumber))
            builder.Append('|')
                   .Append(room.RoomId).Append(':')
                   .Append(room.CheckInDate.DayNumber).Append(':')
                   .Append(room.CheckOutDate.DayNumber).Append(':')
                   .Append(room.Adults).Append(':')
                   .Append(room.Children);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexString(hash);
    }
}
