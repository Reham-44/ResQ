using MediatR;
using ResQ.Application.Features.Emergencies.DTOs;

namespace ResQ.Application.Features.Emergencies.Queries.GetEmergencyById;

public record GetEmergencyByIdQuery(int Id, string UserId, bool IsStaff) : IRequest<EmergencyDetailDto?>;
