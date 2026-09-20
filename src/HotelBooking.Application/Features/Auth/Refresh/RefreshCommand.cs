using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Auth.Refresh;

public sealed record RefreshCommand(string RefreshToken) : ICommand<RefreshResponse>;