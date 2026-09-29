using HotelBooking.Application.Features.Reviews.Abstractions;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.Reviews;

public sealed class ReviewRepository : IReviewRepository
{
    private readonly ApplicationDbContext _db;

    public ReviewRepository(ApplicationDbContext db) => _db = db;

    public Task<ReviewBookingInfo?> GetBookingForReviewAsync(Guid bookingId, CancellationToken cancellationToken) =>
        _db.Bookings
            .Where(b => b.Id == bookingId)
            .Select(b => new ReviewBookingInfo(
                b.BookingGroup.UserId,
                b.Room.HotelId,
                b.Status,
                b.CheckOutDate))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> HasReviewAsync(Guid bookingId, CancellationToken cancellationToken) =>
        _db.Reviews.AnyAsync(r => r.BookingId == bookingId, cancellationToken);

    public Task<string?> GetReviewerNameAsync(Guid userId, CancellationToken cancellationToken) =>
        _db.Users
            .Where(u => u.Id == userId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync(cancellationToken);

    public void Add(Review review) => _db.Reviews.Add(review);
}
