using FluentValidation;

namespace HotelBooking.Application.Features.Admin.RoomTypes.Create;

public sealed class CreateRoomTypeCommandValidator : AbstractValidator<CreateRoomTypeCommand>
{
    public CreateRoomTypeCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Description).MaximumLength(500).When(x => x.Description is not null);
    }
}