using MediatR;

namespace ResQ.Application.Features.Emergencies.Commands.MonitorOverdueEmergencies;

public sealed record MonitorOverdueEmergenciesCommand : IRequest<int>;
