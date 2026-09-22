using FluentValidation;

namespace HotelBooking.Application.Features.Admin.Users.Create;

public sealed class CreateAdminUserCommandValidator : AbstractValidator<CreateAdminUserCommand>
{
    private static readonly string[] AllowedRoles = { "User", "Admin" };

    public CreateAdminUserCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.UserName)
            .NotEmpty()
            .MinimumLength(3)
            .MaximumLength(32)
            .Matches("^[a-zA-Z0-9._-]+$")
            .WithMessage("Username may contain only letters, digits, dot, underscore, or hyphen.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(128)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => AllowedRoles.Contains(r))
            .WithMessage("Role must be either 'User' or 'Admin'.");
    }
}