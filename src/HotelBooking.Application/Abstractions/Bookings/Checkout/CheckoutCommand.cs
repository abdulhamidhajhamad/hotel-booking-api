using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Bookings.Checkout;

public sealed record CheckoutCommand(
    string IdempotencyKey,
    string PaymentMethodId,
    string? SpecialRequests,
    IReadOnlyList<CheckoutRoomItem> Rooms)
    : ICommand<CheckoutResult>, IManagesOwnTransactions;