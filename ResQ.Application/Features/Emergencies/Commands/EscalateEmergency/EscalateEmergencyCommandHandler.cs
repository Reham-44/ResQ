using MediatR;
using Microsoft.EntityFrameworkCore;
using ResQ.Application.Common.Interfaces;
using ResQ.Domain.Entities;
using ResQ.Domain.Enums;

namespace ResQ.Application.Features.Emergencies.Commands.EscalateEmergency;

public sealed class EscalateEmergencyCommandHandler(IApplicationDbContext db) : IRequestHandler<EscalateEmergencyCommand, bool>
{
    private static readonly EmergencyStatus[] EscalatableStatuses =
    [
        EmergencyStatus.Created,
        EmergencyStatus.Dispatched,
        EmergencyStatus.Accepted,
        EmergencyStatus.OnTheWay,
        EmergencyStatus.Arrived
    ];

    public async Task<bool> Handle(EscalateEmergencyCommand request, CancellationToken cancellationToken)
    {
        var emergency = await db.Emergencies.FirstOrDefaultAsync(e => e.Id == request.EmergencyId, cancellationToken);
        if (emergency is null || emergency.Status == EmergencyStatus.Escalated ||
            !EscalatableStatuses.Contains(emergency.Status) || emergency.ResponseDeadline > DateTime.UtcNow)
            return false;

        var oldStatus = emergency.Status;
        emergency.Escalate();
        var history = new EmergencyHistory(
            emergency.Id,
            "SlaEscalated",
            "SLA Monitor",
            oldStatus,
            EmergencyStatus.Escalated,
            "Response deadline passed.");
        var notification = new Notification(
            emergency.CitizenId,
            "Emergency escalated",
            $"Emergency {emergency.TrackingNumber} was escalated because its response deadline passed.");
        db.EmergencyHistories.Add(history);
        db.Notifications.Add(notification);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            // The row-version update and both inserts share SaveChanges' transaction.
            // Detach the rolled-back inserts before checking whether another worker won.
            db.EmergencyHistories.Remove(history);
            db.Notifications.Remove(notification);
            var currentStatus = await db.Emergencies.AsNoTracking()
                .Where(e => e.Id == request.EmergencyId)
                .Select(e => (EmergencyStatus?)e.Status)
                .SingleOrDefaultAsync(cancellationToken);

            if (currentStatus is null || currentStatus == EmergencyStatus.Escalated ||
                currentStatus is EmergencyStatus.Resolved or EmergencyStatus.Closed or EmergencyStatus.Cancelled)
                return false;

            throw;
        }
    }
}
