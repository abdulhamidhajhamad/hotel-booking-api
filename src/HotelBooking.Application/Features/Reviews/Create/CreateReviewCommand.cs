using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Features.Reviews.Common;

namespace HotelBooking.Application.Features.Reviews.Create;

public sealed record CreateReviewCommand(
    Guid BookingId,
    int Rating,
    string? Comment) : ICommand<ReviewDto>;