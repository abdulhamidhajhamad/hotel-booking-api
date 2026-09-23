using HotelBooking.Application.Abstractions.Outbox;
using HotelBooking.Application.Common.Messaging;

namespace HotelBooking.Application.Features.Admin.Outbox.GetDeadLetters;

public sealed record GetDeadLettersQuery(int Skip, int Take)
    : IQuery<IReadOnlyList<OutboxDeadLetterDto>>;
