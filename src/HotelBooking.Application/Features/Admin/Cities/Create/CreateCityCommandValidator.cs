using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Cities.Create;

public sealed class CreateCityCommandValidator : AbstractValidator<CreateCityCommand>
{
    public CreateCityCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Country)
            .NotEmpty()
            .Length(2)
            .Matches("^[A-Za-z]{2}$")
            .WithMessage("Country must be a 2-letter ISO code.");

        RuleFor(x => x.PostalCode)
            .MaximumLength(20)
            .When(x => x.PostalCode is not null);

        RuleFor(x => x.Timezone)
            .NotEmpty()
            .MaximumLength(64);
    }
}