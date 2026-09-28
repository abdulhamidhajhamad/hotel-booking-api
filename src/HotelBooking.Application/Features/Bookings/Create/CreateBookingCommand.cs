using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Bookings.Create;

public sealed record CreateBookingCommand(
    string IdempotencyKey,
    string? SpecialRequests,
    IReadOnlyList<CreateBookingRoomItem> Rooms)
    : ICommand<CreateBookingResult>, IManagesOwnTransactions;