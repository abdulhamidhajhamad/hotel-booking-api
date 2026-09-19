using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Amenities.Update;

public sealed class UpdateAmenityCommandValidator : AbstractValidator<UpdateAmenityCommand>
{
    public UpdateAmenityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x)
            .Must(x => x.Name is not null || x.Icon is not null)
            .WithMessage("At least one field must be provided.");

        When(x => x.Name is not null, () =>
            RuleFor(x => x.Name!).NotEmpty().MaximumLength(100));

        When(x => x.Icon is not null, () =>
            RuleFor(x => x.Icon!).MaximumLength(100));
    }
}