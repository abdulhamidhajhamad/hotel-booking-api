using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Hotels.Create;

public sealed class CreateHotelCommandValidator : AbstractValidator<CreateHotelCommand>
{
    public CreateHotelCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000).When(x => x.Description is not null);
        RuleFor(x => x.StarRating).InclusiveBetween(1, 5);
        RuleFor(x => x.Category).IsInEnum();
        RuleFor(x => x.Address).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x.CityId).NotEmpty();
    }
}