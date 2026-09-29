using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Discounts.Abstractions;
using HotelBooking.Application.Features.Admin.Discounts.Common;
using HotelBooking.Domain.Entities;

namespace HotelBooking.Application.Features.Admin.Discounts.Create;

public sealed class CreateDiscountCommandHandler
    : ICommandHandler<CreateDiscountCommand, DiscountDto>
{
    private readonly IDiscountRepository _discounts;

    public CreateDiscountCommandHandler(IDiscountRepository discounts) => _discounts = discounts;

    public async Task<Result<DiscountDto>> Handle(
        CreateDiscountCommand command,
        CancellationToken cancellationToken)
    {
        var room = await _discounts.GetRoomSummaryAsync(command.RoomId, cancellationToken);

        if (room is null)
            return DiscountErrors.RoomNotFound(command.RoomId);

        var discount = new Discount
        {
            RoomId = command.RoomId,
            Percentage = command.Percentage,
            StartUtc = DateTime.SpecifyKind(command.StartUtc, DateTimeKind.Utc),
            EndUtc = DateTime.SpecifyKind(command.EndUtc, DateTimeKind.Utc),
            Title = string.IsNullOrWhiteSpace(command.Title) ? null : command.Title!.Trim(),
        };

        _discounts.Add(discount);

        var now = DateTime.UtcNow;

        return new DiscountDto(
            discount.Id,
            room.Id,
            room.Number,
            room.HotelId,
            room.HotelName,
            discount.Title,
            discount.Percentage,
            discount.StartUtc,
            discount.EndUtc,
            IsActive: now >= discount.StartUtc && now <= discount.EndUtc,
            discount.CreatedAt);
    }
}
