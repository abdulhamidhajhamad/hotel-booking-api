using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Rooms.GetList;

public sealed class GetRoomsQueryValidator : AbstractValidator<GetRoomsQuery>
{
    private static readonly string[] AllowedSortFields =
        { "number", "price", "adults", "children", "createdat" };

    public GetRoomsQueryValidator()
    {
        RuleFor(x => x.HotelId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);

        RuleFor(x => x.MinAdults).GreaterThanOrEqualTo(1).When(x => x.MinAdults.HasValue);
        RuleFor(x => x.MinChildren).GreaterThanOrEqualTo(0).When(x => x.MinChildren.HasValue);

        RuleFor(x => x.Search).MaximumLength(50).When(x => x.Search is not null);

        RuleFor(x => x.SortBy)
            .Must(s => s is null || AllowedSortFields.Contains(s.ToLowerInvariant()))
            .WithMessage("SortBy must be one of: number, price, adults, children, createdAt.");
    }
}