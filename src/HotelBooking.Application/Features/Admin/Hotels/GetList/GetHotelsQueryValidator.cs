using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Hotels.GetList;

public sealed class GetHotelsQueryValidator : AbstractValidator<GetHotelsQuery>
{
    private static readonly string[] AllowedSortFields = { "name", "starrating", "createdat" };

    public GetHotelsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.MinStar)
            .InclusiveBetween(1, 5)
            .When(x => x.MinStar.HasValue);

        RuleFor(x => x.Category)
            .IsInEnum()
            .When(x => x.Category.HasValue);

        RuleFor(x => x.SortBy)
            .Must(s => s is null || AllowedSortFields.Contains(s.ToLowerInvariant()))
            .WithMessage("SortBy must be one of: name, starRating, createdAt.");

        RuleFor(x => x.Search)
            .MaximumLength(200)
            .When(x => x.Search is not null);
    }
}