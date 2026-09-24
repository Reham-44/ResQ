using MediatR;
using Microsoft.EntityFrameworkCore;
using ResQ.Application.Common.Interfaces;
using ResQ.Application.Features.Emergencies.Commands.EscalateEmergency;
using ResQ.Domain.Enums;

namespace ResQ.Application.Features.Emergencies.Commands.MonitorOverdueEmergencies;

public sealed class MonitorOverdueEmergenciesCommandHandler(IApplicationDbContext db, ISender sender)
    : IRequestHandler<MonitorOverdueEmergenciesCommand, int>
{
    private static readonly EmergencyStatus[] ActiveStatuses =
    [
        EmergencyStatus.Created,
        EmergencyStatus.Dispatched,
        EmergencyStatus.Accepted,
        EmergencyStatus.OnTheWay,
        EmergencyStatus.Arrived
    ];

    public async Task<int> Handle(MonitorOverdueEmergenciesCommand request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var overdueIds = await db.Emergencies.AsNoTracking()
            .Where(e => ActiveStatuses.Contains(e.Status) && e.ResponseDeadline <= now)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        var escalatedCount = 0;
        foreach (var emergencyId in overdueIds)
        {
            if (await sender.Send(new EscalateEmergencyCommand(emergencyId), cancellationToken))
                escalatedCount++;
        }

        return escalatedCount;
    }
}
