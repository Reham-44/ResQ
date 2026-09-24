using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ResQ.Application.Common.Interfaces;

namespace ResQ.Application.Features.Emergencies.Commands.CreateEmergency;

public class CreateEmergencyCommandValidator : AbstractValidator<CreateEmergencyCommand>
{
    public CreateEmergencyCommandValidator(IApplicationDbContext context)
    {
        RuleFor(x => x.EmergencyTypeId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("A valid EmergencyTypeId is required.")
            .MustAsync((id, cancellationToken) => context.EmergencyTypes
                .AnyAsync(emergencyType => emergencyType.Id == id, cancellationToken))
            .WithMessage("EmergencyTypeId must refer to an existing emergency type.");

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
