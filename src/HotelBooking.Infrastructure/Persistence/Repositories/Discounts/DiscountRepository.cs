using HotelBooking.Application.Features.Admin.Discounts.Abstractions;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Persistence.Repositories.Discounts;

public sealed class DiscountRepository : IDiscountRepository
{
    private readonly ApplicationDbContext _db;

    public DiscountRepository(ApplicationDbContext db) => _db = db;

    public Task<DiscountRoomSummary?> GetRoomSummaryAsync(Guid roomId, CancellationToken cancellationToken) =>
        _db.Rooms
            .AsNoTracking()
            .Where(r => r.Id == roomId)
            .Select(r => new DiscountRoomSummary(r.Id, r.Number, r.HotelId, r.Hotel.Name))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetExistingRoomIdsAsync(IReadOnlyCollection<Guid> roomIds, CancellationToken cancellationToken) =>
        await _db.Rooms
            .Where(r => roomIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

    public Task<Discount?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Discounts.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

    public void Add(Discount discount) => _db.Discounts.Add(discount);

    public void Remove(Discount discount) => _db.Discounts.Remove(discount);
}
