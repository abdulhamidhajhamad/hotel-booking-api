namespace HotelBooking.Application.Features.Auth.Register;

public sealed record RegisterResponse(Guid UserId, string Email, string UserName);