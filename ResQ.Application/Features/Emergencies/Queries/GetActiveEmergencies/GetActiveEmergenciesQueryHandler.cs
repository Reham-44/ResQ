using MediatR;
using Microsoft.EntityFrameworkCore;
using ResQ.Application.Common.Interfaces;
using ResQ.Application.Features.Emergencies.DTOs;
using ResQ.Domain.Enums;

namespace ResQ.Application.Features.Emergencies.Queries.GetActiveEmergencies;

public class GetActiveEmergenciesQueryHandler : IRequestHandler<GetActiveEmergenciesQuery, IReadOnlyList<EmergencySummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetActiveEmergenciesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<EmergencySummaryDto>> Handle(GetActiveEmergenciesQuery request, CancellationToken cancellationToken)
    {
        return await _context.Emergencies
            .AsNoTracking()
            .Where(e => e.Status != EmergencyStatus.Closed && e.Status != EmergencyStatus.Cancelled)
            .OrderByDescending(e => e.Priority)
            .ThenBy(e => e.CreatedAt)
            .Select(e => new EmergencySummaryDto(
                e.Id,
                e.TrackingNumber,
                e.CitizenId,
                e.EmergencyTypeId,
                e.EmergencyType.Name,
                e.Description,
                e.Latitude,
                e.Longitude,
                e.Priority.ToString(),
                e.Status.ToString(),
                e.CreatedAt,
                e.ResponseDeadline,
                e.ResolvedAt))
            .ToListAsync(cancellationToken);
    }
}
