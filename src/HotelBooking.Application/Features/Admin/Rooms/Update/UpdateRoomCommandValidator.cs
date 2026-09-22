using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Rooms.Update;

public sealed class UpdateRoomCommandValidator : AbstractValidator<UpdateRoomCommand>
{
    public UpdateRoomCommandValidator()
    {
        RuleFor(x => x.HotelId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x)
            .Must(x =>
                x.RoomTypeId.HasValue ||
                x.Number is not null ||
                x.AdultsCapacity.HasValue ||
                x.ChildrenCapacity.HasValue ||
                x.PricePerNight.HasValue ||
                x.IsActive.HasValue)
            .WithMessage("At least one field must be provided.");

        When(x => x.RoomTypeId.HasValue, () =>
            RuleFor(x => x.RoomTypeId!.Value).NotEmpty());

        When(x => x.Number is not null, () =>
            RuleFor(x => x.Number!).NotEmpty().MaximumLength(20));

        When(x => x.AdultsCapacity.HasValue, () =>
            RuleFor(x => x.AdultsCapacity!.Value).GreaterThanOrEqualTo(1));

        When(x => x.ChildrenCapacity.HasValue, () =>
            RuleFor(x => x.ChildrenCapacity!.Value).GreaterThanOrEqualTo(0));

        When(x => x.PricePerNight.HasValue, () =>
            RuleFor(x => x.PricePerNight!.Value).GreaterThanOrEqualTo(0));
    }
}