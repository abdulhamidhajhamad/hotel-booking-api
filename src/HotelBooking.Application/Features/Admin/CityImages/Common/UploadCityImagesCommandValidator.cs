using FluentValidation;

namespace HotelBooking.Application.Features.Admin.CityImages.Upload;

public sealed class UploadCityImagesCommandValidator
    : AbstractValidator<UploadCityImagesCommand>
{
    private static readonly string[] AllowedTypes =
        { "image/jpeg", "image/png", "image/webp" };

    public UploadCityImagesCommandValidator()
    {
        RuleFor(x => x.CityId).NotEmpty();

        RuleFor(x => x.Files)
            .NotEmpty()
            .Must(f => f.Count <= 20)
            .WithMessage("Cannot upload more than 20 files at once.");

        RuleForEach(x => x.Files).ChildRules(f =>
        {
            f.RuleFor(x => x.ContentType)
                .Must(t => AllowedTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
                .WithMessage("Only JPEG, PNG or WebP images are allowed.");
        });
    }
}