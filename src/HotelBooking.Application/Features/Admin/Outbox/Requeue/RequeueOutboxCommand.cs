using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Admin.Outbox.Requeue;

public sealed record RequeueOutboxCommand(Guid MessageId) : ICommand;
