using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Cities.GetList;

public sealed class GetCitiesQueryValidator : AbstractValidator<GetCitiesQuery>
{
    private static readonly string[] AllowedSortFields = { "name", "country", "createdat" };

    public GetCitiesQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.SortBy)
            .Must(s => s is null || AllowedSortFields.Contains(s.ToLowerInvariant()))
            .WithMessage("SortBy must be one of: name, country, createdAt.");

        RuleFor(x => x.Search)
            .MaximumLength(100)
            .When(x => x.Search is not null);
    }
}