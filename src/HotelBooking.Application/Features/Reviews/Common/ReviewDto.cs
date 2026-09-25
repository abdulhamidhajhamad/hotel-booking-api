namespace HotelBooking.Application.Features.Reviews.Common;

public sealed record ReviewDto(
    Guid Id,
    Guid HotelId,
    int Rating,
    string? Comment,
    string? ReviewerName,
    DateTimeOffset CreatedAt);