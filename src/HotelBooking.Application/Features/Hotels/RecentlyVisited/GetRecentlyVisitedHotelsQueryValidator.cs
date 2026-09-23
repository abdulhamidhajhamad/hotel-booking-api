using FluentValidation;

namespace HotelBooking.Application.Features.Hotels.RecentlyVisited;

public sealed class GetRecentlyVisitedHotelsQueryValidator
    : AbstractValidator<GetRecentlyVisitedHotelsQuery>
{
    public GetRecentlyVisitedHotelsQueryValidator()
    {
        RuleFor(x => x.Count).InclusiveBetween(1, 5);
    }
}