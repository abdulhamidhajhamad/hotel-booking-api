using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Auth.Register;

public sealed record RegisterCommand(
    string Email,
    string FullName,
    string Password) : ICommand<RegisterResponse>;