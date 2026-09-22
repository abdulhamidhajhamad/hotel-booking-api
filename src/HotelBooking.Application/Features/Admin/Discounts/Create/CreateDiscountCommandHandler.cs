using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Discounts.Common;
using HotelBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Discounts.Create;

public sealed class CreateDiscountCommandHandler
    : ICommandHandler<CreateDiscountCommand, DiscountDto>
{
    private readonly IApplicationDbContext _db;

    public CreateDiscountCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result<DiscountDto>> Handle(
        CreateDiscountCommand command,
        CancellationToken cancellationToken)
    {
        var room = await _db.Rooms
            .AsNoTracking()
            .Where(r => r.Id == command.RoomId)
            .Select(r => new { r.Id, r.Number, r.HotelId, HotelName = r.Hotel.Name })
            .FirstOrDefaultAsync(cancellationToken);

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

        await _db.Discounts.AddAsync(discount, cancellationToken);

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