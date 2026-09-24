using FluentValidation;
using MediatR;

namespace ResQ.Application.Features.Emergencies.Commands.EscalateEmergency;

public sealed record EscalateEmergencyCommand(int EmergencyId) : IRequest<bool>;

public sealed class EscalateEmergencyCommandValidator : AbstractValidator<EscalateEmergencyCommand>
{
    public EscalateEmergencyCommandValidator() => RuleFor(x => x.EmergencyId).GreaterThan(0);
}
