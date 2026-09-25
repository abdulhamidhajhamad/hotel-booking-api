using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Reviews.Common;
using HotelBooking.Domain.Common;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Reviews.Create;

public sealed class CreateReviewCommandHandler
    : ICommandHandler<CreateReviewCommand, ReviewDto>
{
    private readonly IApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _clock;

    public CreateReviewCommandHandler(
        IApplicationDbContext db,
        ICurrentUser currentUser,
        TimeProvider clock)
    {
        _db = db;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<Result<ReviewDto>> Handle(
        CreateReviewCommand command,
        CancellationToken cancellationToken)
    {
        if (_currentUser.Id is not { } userId)
            return ReviewErrors.NotAuthenticated();

        var booking = await _db.Bookings
            .Where(b => b.Id == command.BookingId)
            .Select(b => new
            {
                OwnerId = b.BookingGroup.UserId,
                HotelId = b.Room.HotelId,
                b.Status,
                b.CheckOutDate
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (booking is null)
            return ReviewErrors.BookingNotFound(command.BookingId);

        if (booking.OwnerId != userId)
            return ReviewErrors.NotBookingOwner();

        var today = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);

        if (booking.Status != BookingStatus.Confirmed || booking.CheckOutDate >= today)
            return ReviewErrors.StayNotCompleted();

        var alreadyReviewed = await _db.Reviews
            .AnyAsync(r => r.BookingId == command.BookingId, cancellationToken);

        if (alreadyReviewed)
            return ReviewErrors.AlreadyReviewed();

        var reviewerName = await _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);

        var review = new Review
        {
            BookingId = command.BookingId,
            HotelId = booking.HotelId,
            UserId = userId,
            Rating = command.Rating,
            Comment = string.IsNullOrWhiteSpace(command.Comment) ? null : command.Comment.Trim(),
        };

        await _db.Reviews.AddAsync(review, cancellationToken);

        return new ReviewDto(
            review.Id,
            review.HotelId,
            review.Rating,
            review.Comment,
            reviewerName,
            review.CreatedAt);
    }
}