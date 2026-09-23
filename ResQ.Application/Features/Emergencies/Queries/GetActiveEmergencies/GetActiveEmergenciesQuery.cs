using MediatR;
using ResQ.Application.Features.Emergencies.DTOs;

namespace ResQ.Application.Features.Emergencies.Queries.GetActiveEmergencies;

public record GetActiveEmergenciesQuery() : IRequest<IReadOnlyList<EmergencySummaryDto>>;
