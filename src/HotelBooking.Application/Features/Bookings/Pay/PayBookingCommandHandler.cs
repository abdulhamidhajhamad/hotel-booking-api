using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Application.Abstractions.Payments;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Bookings.Common;
using HotelBooking.Application.Features.Bookings.Pay.Abstractions;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Bookings.Pay;

public sealed class PayBookingCommandHandler : ICommandHandler<PayBookingCommand, PayBookingResult>
{
    private readonly IPayBookingRepository _bookings;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGateway _paymentGateway;
    private readonly ICurrentUser _currentUser;
    private readonly IOutbox _outbox;

    public PayBookingCommandHandler(
        IPayBookingRepository bookings,
        IUnitOfWork unitOfWork,
        IPaymentGateway paymentGateway,
        ICurrentUser currentUser,
        IOutbox outbox)
    {
        _bookings = bookings;
        _unitOfWork = unitOfWork;
        _paymentGateway = paymentGateway;
        _currentUser = currentUser;
        _outbox = outbox;
    }

    public async Task<Result<PayBookingResult>> Handle(
        PayBookingCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return Result<PayBookingResult>.Failure(BookingErrors.NotAuthenticated());

        var group = await _bookings.GetGroupWithBookingsAndPaymentsAsync(command.BookingGroupId, cancellationToken);

        if (group is null)
            return Result<PayBookingResult>.Failure(BookingErrors.BookingGroupNotFound(command.BookingGroupId));

        if (group.UserId != userId)
            return Result<PayBookingResult>.Failure(BookingErrors.BookingForbidden());

        if (group.Payments.Any(p => p.Status == PaymentStatus.Succeeded))
            return Result<PayBookingResult>.Success(new PayBookingResult(
                group.Id, group.ConfirmationNumber, PaymentStatus.Succeeded.ToString(), group.TotalPrice));

        if (group.Bookings.All(b => b.Status != BookingStatus.Pending))
            return Result<PayBookingResult>.Failure(BookingErrors.BookingNotPending());

        if (group.CreatedAt.AddMinutes(BookingHold.TtlMinutes) < DateTimeOffset.UtcNow)
            return Result<PayBookingResult>.Failure(BookingErrors.HoldExpired());

        var idempotencyKey = $"pay-{group.Id:N}";

        var payment = new Payment
        {
            BookingGroupId = group.Id,
            Amount = group.TotalPrice,
            Currency = "USD",
            Provider = "Stripe",
            IdempotencyKey = idempotencyKey,
            Status = PaymentStatus.Pending,
        };
        _bookings.AddPayment(payment);

        var intent = await _paymentGateway.CreateIntentAsync(
            new CreatePaymentIntentRequest(
                group.TotalPrice, "USD", idempotencyKey, $"Booking {group.ConfirmationNumber}"),
            cancellationToken);

        payment.ProviderTransactionId = intent.PaymentIntentId;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var confirmation = await _paymentGateway.ConfirmIntentAsync(
            new ConfirmPaymentIntentRequest(
                intent.PaymentIntentId, command.PaymentMethodId, idempotencyKey),
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
            await _bookings.RemoveHoldsForBookingsAsync(bookingIds, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return confirmation.Succeeded
            ? Result<PayBookingResult>.Success(new PayBookingResult(
                group.Id, group.ConfirmationNumber, PaymentStatus.Succeeded.ToString(), group.TotalPrice))
            : Result<PayBookingResult>.Failure(BookingErrors.PaymentFailed(confirmation.FailureReason));
    }
}
