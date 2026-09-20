using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Auth.Login;

public sealed record LoginCommand(
    string Email,
    string Password) : ICommand<LoginResponse>;