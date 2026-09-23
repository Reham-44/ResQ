using MediatR;
using ResQ.Application.Common.Models;
using ResQ.Application.Features.Emergencies.DTOs;

namespace ResQ.Application.Features.Emergencies.Queries.GetMyEmergencies;

public record GetMyEmergenciesQuery(
    string CitizenId,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<PagedResult<EmergencySummaryDto>>;
