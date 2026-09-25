using FluentValidation;

namespace HotelBooking.Application.Features.Reviews.GetForHotel;

public sealed class GetHotelReviewsQueryValidator : AbstractValidator<GetHotelReviewsQuery>
{
    public GetHotelReviewsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}