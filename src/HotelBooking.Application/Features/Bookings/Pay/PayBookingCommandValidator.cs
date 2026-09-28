using FluentValidation;

namespace HotelBooking.Application.Features.Bookings.Pay;

public sealed class PayBookingCommandValidator : AbstractValidator<PayBookingCommand>
{
    public PayBookingCommandValidator()
    {
        RuleFor(x => x.BookingGroupId).NotEmpty();
        RuleFor(x => x.PaymentMethodId).NotEmpty();
    }
}