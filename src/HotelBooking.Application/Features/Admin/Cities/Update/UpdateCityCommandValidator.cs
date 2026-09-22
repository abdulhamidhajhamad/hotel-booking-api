using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Cities.Update;

public sealed class UpdateCityCommandValidator : AbstractValidator<UpdateCityCommand>
{
    public UpdateCityCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x)
            .Must(x =>
                x.Name is not null ||
                x.Country is not null ||
                x.PostalCode is not null ||
                x.Timezone is not null)
            .WithMessage("At least one field must be provided.");

        When(x => x.Name is not null, () =>
        {
            RuleFor(x => x.Name!)
                .NotEmpty()
                .MaximumLength(100);
        });

        When(x => x.Country is not null, () =>
        {
            RuleFor(x => x.Country!)
                .NotEmpty()
                .Length(2)
                .Matches("^[A-Za-z]{2}$")
                .WithMessage("Country must be a 2-letter ISO code.");
        });

        When(x => x.PostalCode is not null, () =>
        {
            RuleFor(x => x.PostalCode!)
                .MaximumLength(20);
        });

        When(x => x.Timezone is not null, () =>
        {
            RuleFor(x => x.Timezone!)
                .NotEmpty()
                .MaximumLength(64);
        });
    }
}