using FluentValidation;

namespace HotelBooking.Application.Features.Cities.GetTrending;

public sealed class GetTrendingDestinationsQueryValidator
    : AbstractValidator<GetTrendingDestinationsQuery>
{
    public GetTrendingDestinationsQueryValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 5);
    }
}