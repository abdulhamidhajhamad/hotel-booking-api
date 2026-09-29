using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Discounts.Abstractions;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Admin.Discounts.CreateBulk;

public sealed class CreateDiscountsBulkCommandHandler
    : ICommandHandler<CreateDiscountsBulkCommand, BulkCreateResponse>
{
    private readonly IDiscountRepository _discounts;
    private readonly BulkDiscountItemValidator _itemValidator = new();

    public CreateDiscountsBulkCommandHandler(IDiscountRepository discounts) => _discounts = discounts;

    public async Task<Result<BulkCreateResponse>> Handle(
        CreateDiscountsBulkCommand command,
        CancellationToken cancellationToken)
    {
        var errors = new List<BulkErrorItem>();

        for (var i = 0; i < command.Items.Count; i++)
        {
            var validation = _itemValidator.Validate(command.Items[i]);
            if (!validation.IsValid)
            {
                var reason = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage));
                errors.Add(new BulkErrorItem(i, command.Items[i].RoomId, reason));
            }
        }

        var roomIds = command.Items.Select(x => x.RoomId).Distinct().ToArray();

        var existingRoomIds = await _discounts.GetExistingRoomIdsAsync(roomIds, cancellationToken);

        var existingSet = existingRoomIds.ToHashSet();

        for (var i = 0; i < command.Items.Count; i++)
        {
            if (!existingSet.Contains(command.Items[i].RoomId))
                errors.Add(new BulkErrorItem(i, command.Items[i].RoomId, "Room not found."));
        }

        if (errors.Count > 0)
        {
            return new BulkCreateResponse(
                Array.Empty<BulkCreatedItem>(),
                errors);
        }

        var created = new List<BulkCreatedItem>(command.Items.Count);
        for (var i = 0; i < command.Items.Count; i++)
        {
            var item = command.Items[i];
            var discount = new Discount
            {
                RoomId = item.RoomId,
                Percentage = item.Percentage,
                StartUtc = DateTime.SpecifyKind(item.StartUtc, DateTimeKind.Utc),
                EndUtc = DateTime.SpecifyKind(item.EndUtc, DateTimeKind.Utc),
                Title = string.IsNullOrWhiteSpace(item.Title) ? null : item.Title!.Trim(),
            };
            _discounts.Add(discount);
            created.Add(new BulkCreatedItem(i, discount.Id));
        }

        return new BulkCreateResponse(created, Array.Empty<BulkErrorItem>());
    }
}
