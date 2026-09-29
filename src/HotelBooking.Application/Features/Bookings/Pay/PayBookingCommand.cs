using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Bookings.Pay;

public sealed record PayBookingCommand(
    Guid BookingGroupId,
    string PaymentMethodId)
    : ICommand<PayBookingResult>, IManagesOwnTransactions;