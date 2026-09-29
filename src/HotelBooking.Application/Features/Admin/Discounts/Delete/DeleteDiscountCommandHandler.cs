using HotelBooking.Application.Common.Messaging;
using HotelBooking.Application.Common.Results;
using HotelBooking.Application.Features.Admin.Discounts.Abstractions;
using HotelBooking.Application.Features.Admin.Discounts.Common;

namespace HotelBooking.Application.Features.Admin.Discounts.Delete;

public sealed class DeleteDiscountCommandHandler : ICommandHandler<DeleteDiscountCommand>
{
    private readonly IDiscountRepository _discounts;

    public DeleteDiscountCommandHandler(IDiscountRepository discounts) => _discounts = discounts;

    public async Task<Result> Handle(
        DeleteDiscountCommand command,
        CancellationToken cancellationToken)
    {
        var discount = await _discounts.GetByIdAsync(command.Id, cancellationToken);

        if (discount is null)
            return Result.Failure(DiscountErrors.NotFound(command.Id));

        _discounts.Remove(discount);

        return Result.Success();
    }
}
