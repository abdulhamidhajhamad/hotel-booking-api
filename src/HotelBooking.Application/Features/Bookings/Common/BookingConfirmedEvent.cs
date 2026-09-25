using HotelBooking.Application.Abstractions.Outbox;

namespace HotelBooking.Application.Features.Bookings.Common;

public sealed record BookingConfirmedEvent(Guid BookingGroupId) : IIntegrationEvent;