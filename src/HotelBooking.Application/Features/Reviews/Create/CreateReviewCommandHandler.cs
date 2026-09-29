using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Reviews.Abstractions;
using HotelBooking.Application.Features.Reviews.Common;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Reviews.Create;

public sealed class CreateReviewCommandHandler
    : ICommandHandler<CreateReviewCommand, ReviewDto>
{
    private readonly IReviewRepository _reviews;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;

    public CreateReviewCommandHandler(
        IReviewRepository reviews,
        ICurrentUser currentUser,
        TimeProvider clock)
    {
        _reviews = reviews;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<ReviewDto>> Handle(
        CreateReviewCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return ReviewErrors.NotAuthenticated();

        var booking = await _reviews.GetBookingForReviewAsync(command.BookingId, cancellationToken);

        if (booking is null)
            return ReviewErrors.BookingNotFound(command.BookingId);

        if (booking.OwnerId != userId)
            return ReviewErrors.NotBookingOwner();

        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);

        if (booking.Status != BookingStatus.Confirmed || booking.CheckOutDate >= today)
            return ReviewErrors.StayNotCompleted();

        var alreadyReviewed = await _reviews.HasReviewAsync(command.BookingId, cancellationToken);

        if (alreadyReviewed)
            return ReviewErrors.AlreadyReviewed();

        var reviewerName = await _reviews.GetReviewerNameAsync(userId, cancellationToken);

        var review = new Review
        {
            BookingId = command.BookingId,
            HotelId = booking.HotelId,
            UserId = userId,
            Rating = command.Rating,
            Comment = string.IsNullOrWhiteSpace(command.Comment) ? null : command.Comment.Trim(),
        };

        _reviews.Add(review);

        return new ReviewDto(
            review.Id,
            review.HotelId,
            review.Rating,
            review.Comment,
            reviewerName,
            review.CreatedAt);
    }
}
