using FluentValidation;

namespace HotelBooking.Application.Features.Bookings.Checkout;

public sealed class CheckoutCommandValidator : AbstractValidator<CheckoutCommand>
{
    public CheckoutCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.PaymentMethodId).NotEmpty();
        RuleFor(x => x.SpecialRequests).MaximumLength(1000);
        RuleFor(x => x.Rooms).NotEmpty();

        RuleForEach(x => x.Rooms).ChildRules(room =>
        {
            room.RuleFor(r => r.RoomId).NotEmpty();
            room.RuleFor(r => r.CheckOutDate).GreaterThan(r => r.CheckInDate);
            room.RuleFor(r => r.Adults).GreaterThanOrEqualTo(1);
            room.RuleFor(r => r.Children).GreaterThanOrEqualTo(0);
        });
    }
}