using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Infrastructure.Outbox;

public sealed class OutboxAdmin : IOutboxAdmin
{
    private readonly ApplicationDbContext _db;

    public OutboxAdmin(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<OutboxDeadLetterDto>> GetDeadLettersAsync(
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        return await _db.OutboxMessages
            .Where(m => m.Status == OutboxMessageStatus.DeadLetter)
            .OrderByDescending(m => m.OccurredAtUtc)
            .Skip(skip)
            .Take(take)
            .Select(m => new OutboxDeadLetterDto(
                m.Id,
                m.Type,
                m.OccurredAtUtc,
                m.AttemptCount,
                m.LastError))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RequeueAsync(Guid messageId, CancellationToken cancellationToken)
    {
        var row = await _db.OutboxMessages
            .FirstOrDefaultAsync(
                m => m.Id == messageId && m.Status == OutboxMessageStatus.DeadLetter,
                cancellationToken);

        if (row is null)
            return false;

        row.Status = OutboxMessageStatus.Pending;
        row.AttemptCount = 0;
        row.NextAttemptAtUtc = DateTimeOffset.UtcNow;
        row.LastError = null;

        return true;
    }
}
