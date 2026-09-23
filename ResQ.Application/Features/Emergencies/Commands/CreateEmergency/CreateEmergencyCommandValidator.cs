using FluentValidation;

namespace ResQ.Application.Features.Emergencies.Commands.CreateEmergency;

public class CreateEmergencyCommandValidator : AbstractValidator<CreateEmergencyCommand>
{
    public CreateEmergencyCommandValidator()
    {
        RuleFor(x => x.EmergencyTypeId)
            .GreaterThan(0).WithMessage("A valid EmergencyTypeId is required.");

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90.0, 90.0).WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180.0, 180.0).WithMessage("Longitude must be between -180 and 180.");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("A valid EmergencyPriority is required.");

        RuleFor(x => x.CitizenId)
            .NotEmpty().WithMessage("CitizenId is required from authenticated user.");
    }
}
