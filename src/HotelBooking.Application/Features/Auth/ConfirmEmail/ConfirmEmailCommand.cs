using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Auth.ConfirmEmail;

public sealed record ConfirmEmailCommand(string Token) : ICommand;
