using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Amenities.Create;

public sealed class CreateAmenityCommandValidator : AbstractValidator<CreateAmenityCommand>
{
    public CreateAmenityCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Icon).MaximumLength(100).When(x => x.Icon is not null);
    }
}