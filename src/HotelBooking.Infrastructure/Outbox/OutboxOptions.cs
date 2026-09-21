namespace HotelBooking.Infrastructure.Outbox;

public sealed class OutboxOptions
{
    public int BatchSize { get; init; } = 20;
    public int PollFallbackSeconds { get; init; } = 60;
    public int MaxAttempts { get; init; } = 8;
    public int BackoffBaseSeconds { get; init; } = 30;
    public int BackoffMaxSeconds { get; init; } = 3600;
}
