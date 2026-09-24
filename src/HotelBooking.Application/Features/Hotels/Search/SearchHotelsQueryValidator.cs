using FluentValidation;

namespace HotelBooking.Application.Features.Hotels.Search;

public sealed class SearchHotelsQueryValidator : AbstractValidator<SearchHotelsQuery>
{
    private static readonly string[] AllowedSortFields = { "name", "price", "starrating" };

    public SearchHotelsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.Adults).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Children).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Rooms).GreaterThanOrEqualTo(1);

        RuleFor(x => x.Query)
            .MaximumLength(200)
            .When(x => x.Query is not null);

        RuleFor(x => x.MinStar)
            .InclusiveBetween(1, 5)
            .When(x => x.MinStar.HasValue);

        RuleFor(x => x.Category)
            .IsInEnum()
            .When(x => x.Category.HasValue);

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinPrice.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MaxPrice.HasValue);

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(x => x.MinPrice!.Value)
            .When(x => x.MinPrice.HasValue && x.MaxPrice.HasValue)
            .WithMessage("MaxPrice must be greater than or equal to MinPrice.");

        RuleFor(x => x.SortBy)
            .Must(s => s is null || AllowedSortFields.Contains(s.ToLowerInvariant()))
            .WithMessage("SortBy must be one of: name, price, starRating.");

        RuleFor(x => x.CheckIn)
            .NotNull()
            .When(x => x.CheckOut.HasValue)
            .WithMessage("CheckIn is required when CheckOut is provided.");

        RuleFor(x => x.CheckOut)
            .NotNull()
            .When(x => x.CheckIn.HasValue)
            .WithMessage("CheckOut is required when CheckIn is provided.");

        RuleFor(x => x.CheckOut)
            .GreaterThan(x => x.CheckIn!.Value)
            .When(x => x.CheckIn.HasValue && x.CheckOut.HasValue)
            .WithMessage("CheckOut must be after CheckIn.");
    }
}