namespace HotelBooking.Application.Abstractions.Outbox;

public sealed record OutboxDeadLetterDto(
    Guid Id,
    string Type,
    DateTimeOffset OccurredAtUtc,
    int AttemptCount,
    string? LastError);

public interface IOutboxAdmin
{
    Task<IReadOnlyList<OutboxDeadLetterDto>> GetDeadLettersAsync(
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<bool> RequeueAsync(Guid messageId, CancellationToken cancellationToken);
}
