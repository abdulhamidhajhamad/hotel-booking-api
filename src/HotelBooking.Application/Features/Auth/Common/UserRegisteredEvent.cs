using HotelBooking.Application.Abstractions.Outbox;

namespace HotelBooking.Application.Features.Auth.Common;

public sealed record UserRegisteredEvent(
    Guid UserId,
    string Email,
    string UserName,
    string ConfirmationToken) : IIntegrationEvent;
