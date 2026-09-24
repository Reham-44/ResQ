using FluentValidation;

namespace ResQ.Application.Features.Auth.Commands.Register;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(8)
            .MaximumLength(100);

        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(r => string.Equals(r, "Citizen", StringComparison.Ordinal))
            .WithMessage("Public registration is restricted to the Citizen role.");
    }
}
