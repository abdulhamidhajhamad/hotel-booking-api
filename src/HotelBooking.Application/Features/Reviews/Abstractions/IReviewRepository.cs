using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Reviews.Abstractions;

public interface IReviewRepository
{
    Task<ReviewBookingInfo?> GetBookingForReviewAsync(Guid bookingId, CancellationToken cancellationToken);

    Task<bool> HasReviewAsync(Guid bookingId, CancellationToken cancellationToken);

    Task<string?> GetReviewerNameAsync(Guid userId, CancellationToken cancellationToken);

    void Add(Review review);
}
