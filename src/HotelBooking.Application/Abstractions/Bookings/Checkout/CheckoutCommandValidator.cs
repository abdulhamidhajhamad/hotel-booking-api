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

        RuleFor(x => x.Rooms)
            .Must(HaveNoOverlappingDatesPerRoom)
            .WithMessage("The same room cannot be booked for overlapping date ranges in one request.");
    }

    private static bool HaveNoOverlappingDatesPerRoom(IReadOnlyList<CheckoutRoomItem> rooms)
    {
        foreach (var perRoom in rooms.GroupBy(r => r.RoomId))
        {
            var ordered = perRoom.OrderBy(r => r.CheckInDate).ToList();
            for (var i = 1; i < ordered.Count; i++)
                if (ordered[i].CheckInDate < ordered[i - 1].CheckOutDate)
                    return false;
        }

        return true;
    }
}