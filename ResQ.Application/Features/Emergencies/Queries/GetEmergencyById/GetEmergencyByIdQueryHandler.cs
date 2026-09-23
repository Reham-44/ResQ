using MediatR;
using Microsoft.EntityFrameworkCore;
using ResQ.Application.Common.Interfaces;
using ResQ.Application.Features.Emergencies.DTOs;

namespace ResQ.Application.Features.Emergencies.Queries.GetEmergencyById;

public class GetEmergencyByIdQueryHandler : IRequestHandler<GetEmergencyByIdQuery, EmergencyDetailDto?>
{
    private readonly IApplicationDbContext _context;

    public GetEmergencyByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EmergencyDetailDto?> Handle(GetEmergencyByIdQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Emergencies
            .AsNoTracking()
            .Where(e => e.Id == request.Id);

        if (!request.IsStaff)
        {
            query = query.Where(e => e.CitizenId == request.UserId);
        }

        return await query
            .Select(e => new EmergencyDetailDto(
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
            .FirstOrDefaultAsync(cancellationToken);
    }
}
