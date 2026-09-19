using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Rooms.Create;

public sealed class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.HotelId).NotEmpty();
        RuleFor(x => x.RoomTypeId).NotEmpty();
        RuleFor(x => x.Number).NotEmpty().MaximumLength(20);
        RuleFor(x => x.AdultsCapacity).GreaterThanOrEqualTo(1);
        RuleFor(x => x.ChildrenCapacity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PricePerNight).GreaterThanOrEqualTo(0);
    }
}