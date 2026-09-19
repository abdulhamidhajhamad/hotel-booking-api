using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Hotels.Update;

public sealed class UpdateHotelCommandValidator : AbstractValidator<UpdateHotelCommand>
{
    public UpdateHotelCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x)
            .Must(x =>
                x.Name is not null ||
                x.Description is not null ||
                x.StarRating.HasValue ||
                x.Category.HasValue ||
                x.Address is not null ||
                x.Latitude.HasValue ||
                x.Longitude.HasValue ||
                x.CityId.HasValue ||
                x.OwnerId.HasValue)
            .WithMessage("At least one field must be provided.");

        When(x => x.Name is not null, () =>
            RuleFor(x => x.Name!).NotEmpty().MaximumLength(200));

        When(x => x.Description is not null, () =>
            RuleFor(x => x.Description!).MaximumLength(2000));

        When(x => x.StarRating.HasValue, () =>
            RuleFor(x => x.StarRating!.Value).InclusiveBetween(1, 5));

        When(x => x.Category.HasValue, () =>
            RuleFor(x => x.Category!.Value).IsInEnum());

        When(x => x.Address is not null, () =>
            RuleFor(x => x.Address!).NotEmpty().MaximumLength(300));

        When(x => x.Latitude.HasValue, () =>
            RuleFor(x => x.Latitude!.Value).InclusiveBetween(-90, 90));

        When(x => x.Longitude.HasValue, () =>
            RuleFor(x => x.Longitude!.Value).InclusiveBetween(-180, 180));

        When(x => x.CityId.HasValue, () =>
            RuleFor(x => x.CityId!.Value).NotEmpty());

        When(x => x.OwnerId.HasValue, () =>
            RuleFor(x => x.OwnerId!.Value).NotEmpty());
    }
}