using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Admin.Outbox.GetDeadLetters;

public sealed class GetDeadLettersQueryHandler
    : IQueryHandler<GetDeadLettersQuery, IReadOnlyList<OutboxDeadLetterDto>>
{
    private readonly IOutboxAdmin _admin;

    public GetDeadLettersQueryHandler(IOutboxAdmin admin) => _admin = admin;

    public async Task<Result<IReadOnlyList<OutboxDeadLetterDto>>> Handle(
        GetDeadLettersQuery query,
        CancellationToken cancellationToken)
    {
        var skip = Math.Max(0, query.Skip);
        var take = Math.Clamp(query.Take <= 0 ? 20 : query.Take, 1, 100);
        var items = await _admin.GetDeadLettersAsync(skip, take, cancellationToken);
        return Result<IReadOnlyList<OutboxDeadLetterDto>>.Success(items);
    }
}
