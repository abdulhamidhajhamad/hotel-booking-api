using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Discounts.CreateBulk;

public sealed class BulkDiscountItemValidator : AbstractValidator<BulkDiscountItem>
{
    public BulkDiscountItemValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty();
        RuleFor(x => x.Percentage).GreaterThan(0m).LessThanOrEqualTo(100m);
        RuleFor(x => x.EndUtc).GreaterThan(x => x.StartUtc)
            .WithMessage("EndUtc must be greater than StartUtc.");
        RuleFor(x => x.Title!).MaximumLength(200).When(x => x.Title is not null);
    }
}