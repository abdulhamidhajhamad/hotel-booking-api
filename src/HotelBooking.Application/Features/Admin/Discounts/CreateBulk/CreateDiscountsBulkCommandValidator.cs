using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Discounts.CreateBulk;

public sealed class CreateDiscountsBulkCommandValidator : AbstractValidator<CreateDiscountsBulkCommand>
{
    public CreateDiscountsBulkCommandValidator()
    {
        RuleFor(x => x.Items).NotNull().NotEmpty()
            .WithMessage("At least one item is required.");
        RuleFor(x => x.Items.Count).LessThanOrEqualTo(100)
            .WithMessage("Bulk request cannot exceed 100 items.");
    }
}