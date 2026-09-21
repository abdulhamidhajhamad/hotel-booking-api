using HotelBooking.Application.Common.Results;

namespace HotelBooking.Application.Features.Admin.Outbox.Common;

public static class OutboxErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound(
            "Outbox.NotFound",
            $"Outbox message '{id}' was not found or is not in a requeuable state.");
}
