using MediatR;
using Microsoft.EntityFrameworkCore;
using ResQ.Application.Common.Interfaces;
using ResQ.Application.Common.Models;
using ResQ.Application.Features.Emergencies.DTOs;

namespace ResQ.Application.Features.Emergencies.Queries.GetMyEmergencies;

public class GetMyEmergenciesQueryHandler : IRequestHandler<GetMyEmergenciesQuery, PagedResult<EmergencySummaryDto>>
{
    private readonly IApplicationDbContext _context;

    public GetMyEmergenciesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<EmergencySummaryDto>> Handle(GetMyEmergenciesQuery request, CancellationToken cancellationToken)
    {
        var pageNumber = request.PageNumber <= 0 ? 1 : request.PageNumber;
        var pageSize = request.PageSize <= 0 ? 10 : (request.PageSize > 100 ? 100 : request.PageSize);

        var query = _context.Emergencies
            .AsNoTracking()
            .Where(e => e.CitizenId == request.CitizenId);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
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

        return new PagedResult<EmergencySummaryDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }
}
