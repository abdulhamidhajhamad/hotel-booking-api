using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Outbox.Common;

namespace HotelBooking.Application.Features.Admin.Outbox.Requeue;

public sealed class RequeueOutboxCommandHandler : ICommandHandler<RequeueOutboxCommand>
{
    private readonly IOutboxAdmin _admin;

    public RequeueOutboxCommandHandler(IOutboxAdmin admin) => _admin = admin;

    public async Task<Result> Handle(
        RequeueOutboxCommand command,
        CancellationToken cancellationToken)
    {
        var requeued = await _admin.RequeueAsync(command.MessageId, cancellationToken);
        if (!requeued)
            return Result.Failure(OutboxErrors.NotFound(command.MessageId));

        return Result.Success();
    }
}
