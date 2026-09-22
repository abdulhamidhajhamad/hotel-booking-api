using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Auth.ResendConfirmation;

public sealed record ResendConfirmationCommand(string Email) : ICommand;
