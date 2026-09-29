using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Admin.Discounts.Abstractions;

public interface IDiscountRepository
{
    Task<DiscountRoomSummary?> GetRoomSummaryAsync(Guid roomId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetExistingRoomIdsAsync(IReadOnlyCollection<Guid> roomIds, CancellationToken cancellationToken);

    Task<Discount?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(Discount discount);

    void Remove(Discount discount);
}
