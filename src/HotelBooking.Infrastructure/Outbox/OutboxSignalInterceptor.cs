using Microsoft.EntityFrameworkCore.Diagnostics;

namespace HotelBooking.Infrastructure.Outbox;

public sealed class OutboxSignalInterceptor : SaveChangesInterceptor
{
    private readonly OutboxSignal _signal;

    public OutboxSignalInterceptor(OutboxSignal signal) => _signal = signal;

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        _signal.Notify();
        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}
