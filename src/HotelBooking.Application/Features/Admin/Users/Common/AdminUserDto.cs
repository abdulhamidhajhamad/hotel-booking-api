namespace HotelBooking.Application.Features.Admin.Users.Common;

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string UserName,
    string Role,
    DateTimeOffset CreatedAt);