using FluentValidation;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Update;

public sealed class UpdateRoomTypeCommandValidator : AbstractValidator<UpdateRoomTypeCommand>
{
    public UpdateRoomTypeCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x)
            .Must(x => x.Name is not null || x.Description is not null)
            .WithMessage("At least one field must be provided.");

        When(x => x.Name is not null, () =>
            RuleFor(x => x.Name!).NotEmpty().MaximumLength(50));

        When(x => x.Description is not null, () =>
            RuleFor(x => x.Description!).MaximumLength(500));
    }
}