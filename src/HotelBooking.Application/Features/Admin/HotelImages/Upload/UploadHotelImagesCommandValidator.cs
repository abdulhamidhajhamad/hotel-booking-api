using FluentValidation;

namespace HotelBooking.Application.Features.Admin.HotelImages.Upload;

public sealed class UploadHotelImagesCommandValidator
    : AbstractValidator<UploadHotelImagesCommand>
{
    private static readonly string[] AllowedTypes =
        { "image/jpeg", "image/png", "image/webp" };

    public UploadHotelImagesCommandValidator()
    {
        RuleFor(x => x.HotelId).NotEmpty();

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