using FluentValidation;

namespace HotelBooking.Application.Features.Home.GetFeaturedDeals;

public sealed class GetFeaturedDealsQueryValidator
    : AbstractValidator<GetFeaturedDealsQuery>
{
    public GetFeaturedDealsQueryValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 5);
    }
}