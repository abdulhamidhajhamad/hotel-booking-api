using System.Security.Cryptography;
using System.Text;
using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Application.Abstractions.Payments;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Bookings.Common;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Bookings.Checkout;

public sealed class CheckoutCommandHandler : ICommandHandler<CheckoutCommand, CheckoutResult>
{
    private readonly IApplicationDbContext _db;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ICurrentUser _currentUser;
    private readonly IOutbox _outbox;

    public CheckoutCommandHandler(
        IApplicationDbContext db,
        IPaymentGateway paymentGateway,
        ICurrentUser currentUser,
        IOutbox outbox)
    {
        _db = db;
        _paymentGateway = paymentGateway;
        _currentUser = currentUser;
        _outbox = outbox;
    }

    public async Task<Result<CheckoutResult>> Handle(
        CheckoutCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Result<CheckoutResult>.Failure(BookingErrors.NotAuthenticated());

        var requestHash = ComputeRequestHash(command, userId);

        var existing = await _db.IdempotencyRecords
            .FirstOrDefaultAsync(r => r.Key == command.IdempotencyKey, cancellationToken);

        if (existing is not null)
            return await ReplayAsync(existing, requestHash, cancellationToken);

        var now = DateTime.UtcNow;
        var roomIds = command.Rooms.Select(r => r.RoomId).Distinct().ToList();

        var pricingByRoom = await _db.Rooms
            .Where(r => roomIds.Contains(r.Id))
            .Select(r => new RoomPricing(
                r.Id,
                r.IsActive,
                r.AdultsCapacity,
                r.ChildrenCapacity,
                r.PricePerNight,
                r.Discounts
                    .Where(d => d.StartUtc <= now && d.EndUtc >= now)
                    .OrderByDescending(d => d.Percentage)
                    .Select(d => (decimal?)d.Percentage)
                    .FirstOrDefault()))
            .ToDictionaryAsync(r => r.RoomId, cancellationToken);

        foreach (var item in command.Rooms)
        {
            if (!pricingByRoom.TryGetValue(item.RoomId, out var pricing))
                return Result<CheckoutResult>.Failure(BookingErrors.RoomNotFound(item.RoomId));
            if (!pricing.IsActive)
                return Result<CheckoutResult>.Failure(BookingErrors.RoomInactive(item.RoomId));
            if (item.Adults > pricing.AdultsCapacity || item.Children > pricing.ChildrenCapacity)
                return Result<CheckoutResult>.Failure(BookingErrors.RoomCapacityExceeded(item.RoomId));
        }

        var requestedSlots = new HashSet<(Guid RoomId, DateOnly Date)>();
        foreach (var item in command.Rooms)
            for (var date = item.CheckInDate; date < item.CheckOutDate; date = date.AddDays(1))
                requestedSlots.Add((item.RoomId, date));

        var takenSlots = await _db.RoomAvailability
            .Where(a => roomIds.Contains(a.RoomId))
            .Select(a => new { a.RoomId, a.Date })
            .ToListAsync(cancellationToken);

        if (takenSlots.Any(t => requestedSlots.Contains((t.RoomId, t.Date))))
            return Result<CheckoutResult>.Failure(BookingErrors.RoomNotAvailable());

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

        var intent = await _paymentGateway.CreateIntentAsync(
            new CreatePaymentIntentRequest(
                groupTotal, "USD", command.IdempotencyKey, $"Booking {group.ConfirmationNumber}"),
            cancellationToken);

        var payment = new Payment
        {
            BookingGroupId = group.Id,
            Amount = groupTotal,
            Currency = "USD",
            Provider = "Stripe",
            ProviderTransactionId = intent.PaymentIntentId,
            IdempotencyKey = command.IdempotencyKey,
            Status = PaymentStatus.Pending,
        };
        group.Payments.Add(payment);

        var record = new IdempotencyRecord
        {
            Key = command.IdempotencyKey,
            RequestHash = requestHash,
            Status = IdempotencyStatus.Pending,
        };

        _db.BookingGroups.Add(group);
        _db.IdempotencyRecords.Add(record);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            var raced = await _db.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.Key == command.IdempotencyKey, cancellationToken);

            return raced is not null
                ? await ReplayAsync(raced, requestHash, cancellationToken)
                : Result<CheckoutResult>.Failure(BookingErrors.RoomNotAvailable());
        }

        var confirmation = await _paymentGateway.ConfirmIntentAsync(
            new ConfirmPaymentIntentRequest(
                intent.PaymentIntentId, command.PaymentMethodId, command.IdempotencyKey),
            cancellationToken);

        var completedAt = DateTimeOffset.UtcNow;

        if (confirmation.Succeeded)
        {
            payment.Status = PaymentStatus.Succeeded;
            payment.ProcessedAt = completedAt;
            foreach (var booking in group.Bookings)
                booking.Status = BookingStatus.Confirmed;

            await _outbox.EnqueueAsync(new BookingConfirmedEvent(group.Id), cancellationToken);
        }
        else
        {
            payment.Status = PaymentStatus.Failed;
            payment.ProcessedAt = completedAt;
            foreach (var booking in group.Bookings)
            {
                booking.Status = BookingStatus.Cancelled;
                _db.RoomAvailability.RemoveRange(booking.AvailabilityHolds);
            }
        }

        record.Status = IdempotencyStatus.Completed;
        record.BookingGroupId = group.Id;
        record.CompletedAt = completedAt;

        await _db.SaveChangesAsync(cancellationToken);

        return confirmation.Succeeded
            ? Result<CheckoutResult>.Success(new CheckoutResult(
                group.Id, group.ConfirmationNumber, PaymentStatus.Succeeded.ToString(), groupTotal))
            : Result<CheckoutResult>.Failure(BookingErrors.PaymentFailed(confirmation.FailureReason));
    }

    private async Task<Result<CheckoutResult>> ReplayAsync(
        IdempotencyRecord record,
        string requestHash,
        CancellationToken cancellationToken)
    {
        if (record.RequestHash != requestHash)
            return Result<CheckoutResult>.Failure(BookingErrors.IdempotencyKeyReused());

        if (record.Status != IdempotencyStatus.Completed || record.BookingGroupId is not { } bookingGroupId)
            return Result<CheckoutResult>.Failure(BookingErrors.RequestInProgress());

        var snapshot = await _db.BookingGroups
            .AsNoTracking()
            .Where(g => g.Id == bookingGroupId)
            .Select(g => new
            {
                g.Id,
                g.ConfirmationNumber,
                g.TotalPrice,
                PaymentStatus = g.Payments
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => p.Status)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (snapshot is null)
            return Result<CheckoutResult>.Failure(BookingErrors.RequestInProgress());

        return snapshot.PaymentStatus == PaymentStatus.Succeeded
            ? Result<CheckoutResult>.Success(new CheckoutResult(
                snapshot.Id, snapshot.ConfirmationNumber, snapshot.PaymentStatus.ToString(), snapshot.TotalPrice))
            : Result<CheckoutResult>.Failure(BookingErrors.PaymentFailed(null));
    }

    private static string NewConfirmationNumber() =>
        $"HB-{Guid.NewGuid():N}"[..11].ToUpperInvariant();

    private static string ComputeRequestHash(CheckoutCommand command, Guid userId)
    {
        var builder = new StringBuilder();
        builder.Append(userId).Append('|')
               .Append(command.PaymentMethodId).Append('|')
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

    private sealed record RoomPricing(
        Guid RoomId,
        bool IsActive,
        int AdultsCapacity,
        int ChildrenCapacity,
        decimal PricePerNight,
        decimal? DiscountPercentage);
}