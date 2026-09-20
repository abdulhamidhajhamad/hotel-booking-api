namespace HotelBooking.Application.Features.Admin.Discounts.CreateBulk;

public sealed record BulkCreateResponse(
    IReadOnlyList<BulkCreatedItem> Created,
    IReadOnlyList<BulkErrorItem> Errors);

public sealed record BulkCreatedItem(int Index, Guid Id);

public sealed record BulkErrorItem(int Index, Guid RoomId, string Reason);