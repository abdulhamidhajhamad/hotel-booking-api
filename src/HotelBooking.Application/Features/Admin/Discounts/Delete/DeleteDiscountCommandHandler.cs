using HotelBooking.Application.Abstractions;
using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Discounts.Common;
using Microsoft.EntityFrameworkCore;

namespace HotelBooking.Application.Features.Admin.Discounts.Delete;

public sealed class DeleteDiscountCommandHandler : ICommandHandler<DeleteDiscountCommand>
{
    private readonly IApplicationDbContext _db;

    public DeleteDiscountCommandHandler(IApplicationDbContext db) => _db = db;

    public async Task<Result> Handle(
        DeleteDiscountCommand command,
        CancellationToken cancellationToken)
    {
        var discount = await _db.Discounts
            .FirstOrDefaultAsync(d => d.Id == command.Id, cancellationToken);

        if (discount is null)
            return Result.Failure(DiscountErrors.NotFound(command.Id));

        _db.Discounts.Remove(discount);

        return Result.Success();
    }
}