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
    private const int PendingHoldTtlMinutes = 15;

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

        var staleThreshold = DateTimeOffset.UtcNow.AddMinutes(-PendingHoldTtlMinutes);

        var existingBySlot = (await _db.RoomAvailability
            .Where(a => roomIds.Contains(a.RoomId))
            .Select(a => new ExistingSlot(
                a.RoomId,
                a.Date,
                a.BookingId,
                a.Booking != null ? a.Booking.Status : (BookingStatus?)null,
                a.Booking != null ? a.Booking.CreatedAt : (DateTimeOffset?)null))
            .ToListAsync(cancellationToken))
            .ToDictionary(s => (s.RoomId, s.Date));

        var reclaimableOldBooking = new Dictionary<(Guid RoomId, DateOnly Date), Guid>();

        foreach (var slot in requestedSlots)
        {
            if (!existingBySlot.TryGetValue(slot, out var occupied))
                continue;

            var isStalePending = occupied.BookingStatus == BookingStatus.Pending
                && occupied.BookingId is not null
                && occupied.BookingCreatedAt is { } createdAt
                && createdAt < staleThreshold;

            if (!isStalePending)
                return Result<CheckoutResult>.Failure(BookingErrors.RoomNotAvailable());

            reclaimableOldBooking[slot] = occupied.BookingId!.Value;
        }

        var group = new BookingGroup
        {
            UserId = userId,
            ConfirmationNumber = NewConfirmationNumber(),
            SpecialRequests = command.SpecialRequests,
        };

        var reclaimPlan = new List<ReclaimPlanItem>();
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
            {
                if (reclaimableOldBooking.TryGetValue((item.RoomId, date), out var oldBookingId))
                {
                    reclaimPlan.Add(new ReclaimPlanItem(item.RoomId, date, oldBookingId, booking.Id));
                    continue;
                }

                booking.AvailabilityHolds.Add(new RoomAvailability
                {
                    RoomId = item.RoomId,
                    Date = date,
                });
            }

            group.Bookings.Add(booking);
        }

        group.TotalPrice = groupTotal;

        var payment = new Payment
        {
            BookingGroupId = group.Id,
            Amount = groupTotal,
            Currency = "USD",
            Provider = "Stripe",
            ProviderTransactionId = null,
            IdempotencyKey = command.IdempotencyKey,
            Status = PaymentStatus.Pending,
        };
        group.Payments.Add(payment);

        var record = new IdempotencyRecord
        {
            Key = command.IdempotencyKey,
            RequestHash = requestHash,
            Status = IdempotencyStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        _db.BookingGroups.Add(group);
        _db.IdempotencyRecords.Add(record);

        var outcome = reclaimPlan.Count == 0
            ? await SaveReservationAsync(cancellationToken)
            : await SaveReservationWithReclaimAsync(reclaimPlan, cancellationToken);

        if (outcome == PersistOutcome.DuplicateKey)
        {
            var raced = await _db.IdempotencyRecords
                .FirstOrDefaultAsync(r => r.Key == command.IdempotencyKey, cancellationToken);

            return raced is not null
                ? await ReplayAsync(raced, requestHash, cancellationToken)
                : Result<CheckoutResult>.Failure(BookingErrors.RoomNotAvailable());
        }

        if (outcome == PersistOutcome.ReclaimLost)
            return Result<CheckoutResult>.Failure(BookingErrors.RoomNotAvailable());

        var intent = await _paymentGateway.CreateIntentAsync(
            new CreatePaymentIntentRequest(
                groupTotal, "USD", command.IdempotencyKey, $"Booking {group.ConfirmationNumber}"),
            cancellationToken);

        payment.ProviderTransactionId = intent.PaymentIntentId;
        await _db.SaveChangesAsync(cancellationToken);

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
                booking.Status = BookingStatus.Cancelled;

            var bookingIds = group.Bookings.Select(b => b.Id).ToList();
            var holds = await _db.RoomAvailability
                .Where(a => a.BookingId != null && bookingIds.Contains(a.BookingId.Value))
                .ToListAsync(cancellationToken);
            _db.RoomAvailability.RemoveRange(holds);
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

    private async Task<PersistOutcome> SaveReservationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return PersistOutcome.Success;
        }
        catch (DbUpdateException)
        {
            return PersistOutcome.DuplicateKey;
        }
    }

    private async Task<PersistOutcome> SaveReservationWithReclaimAsync(
        IReadOnlyList<ReclaimPlanItem> reclaimPlan,
        CancellationToken cancellationToken)
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return PersistOutcome.DuplicateKey;
            }

            foreach (var item in reclaimPlan)
            {
                var affected = await _db.RoomAvailability
                    .Where(a => a.RoomId == item.RoomId
                             && a.Date == item.Date
                             && a.BookingId == item.OldBookingId)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(a => a.BookingId, item.NewBookingId),
                        cancellationToken);

                if (affected != 1)
                    return PersistOutcome.ReclaimLost;
            }

            var oldBookingIds = reclaimPlan.Select(i => i.OldBookingId).Distinct().ToList();
            await _db.Bookings
                .Where(b => oldBookingIds.Contains(b.Id) && b.Status == BookingStatus.Pending)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(b => b.Status, BookingStatus.Cancelled),
                    cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return PersistOutcome.Success;
        });
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

    private enum PersistOutcome
    {
        Success,
        DuplicateKey,
        ReclaimLost
    }

    private sealed record RoomPricing(
        Guid RoomId,
        bool IsActive,
        int AdultsCapacity,
        int ChildrenCapacity,
        decimal PricePerNight,
        decimal? DiscountPercentage);

    private sealed record ExistingSlot(
        Guid RoomId,
        DateOnly Date,
        Guid? BookingId,
        BookingStatus? BookingStatus,
        DateTimeOffset? BookingCreatedAt);

    private sealed record ReclaimPlanItem(
        Guid RoomId,
        DateOnly Date,
        Guid OldBookingId,
        Guid NewBookingId);
}