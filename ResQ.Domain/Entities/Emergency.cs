using ResQ.Domain.Enums;
using ResQ.Domain.Exceptions;

namespace ResQ.Domain.Entities;

public class Emergency
{
    // Valid transitions: from status -> allowed next statuses
    private static readonly Dictionary<EmergencyStatus, IReadOnlyList<EmergencyStatus>> AllowedTransitions =
        new()
        {
            [EmergencyStatus.Created]    = [EmergencyStatus.Dispatched, EmergencyStatus.Cancelled, EmergencyStatus.Escalated],
            [EmergencyStatus.Dispatched] = [EmergencyStatus.Accepted, EmergencyStatus.OnTheWay, EmergencyStatus.Cancelled, EmergencyStatus.Escalated],
            [EmergencyStatus.Accepted]   = [EmergencyStatus.OnTheWay,   EmergencyStatus.Cancelled, EmergencyStatus.Escalated],
            [EmergencyStatus.OnTheWay]   = [EmergencyStatus.Arrived,    EmergencyStatus.Cancelled, EmergencyStatus.Escalated],
            [EmergencyStatus.Arrived]    = [EmergencyStatus.Resolved,   EmergencyStatus.Escalated],
            [EmergencyStatus.Resolved]   = [EmergencyStatus.Closed],
            [EmergencyStatus.Escalated]  = [EmergencyStatus.Dispatched, EmergencyStatus.Cancelled],
            [EmergencyStatus.Closed]     = [],
            [EmergencyStatus.Cancelled]  = [],
        };

    private static readonly HashSet<EmergencyStatus> CancellableStatuses =
        [
            EmergencyStatus.Created,
            EmergencyStatus.Dispatched,
            EmergencyStatus.Accepted,
            EmergencyStatus.OnTheWay,
            EmergencyStatus.Escalated
        ];

    private static readonly HashSet<EmergencyStatus> ReassignableStatuses =
        [
            EmergencyStatus.Dispatched,
            EmergencyStatus.Accepted,
            EmergencyStatus.Escalated
        ];

    public int Id { get; private set; }
    public string TrackingNumber { get; private set; } = string.Empty;
    public string CitizenId { get; private set; } = string.Empty;
    public int EmergencyTypeId { get; private set; }
    public string? Description { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public EmergencyPriority Priority { get; private set; }
    public EmergencyStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ResponseDeadline { get; private set; }
    public DateTime? ResolvedAt { get; private set; }

    // Optimistic concurrency token for emergency workflow updates.
    public byte[] RowVersion { get; private set; } = [];

    // Navigation properties
    public EmergencyType EmergencyType { get; private set; } = null!;
    public IReadOnlyCollection<Mission> Missions => _missions.AsReadOnly();
    public IReadOnlyCollection<EmergencyHistory> History => _history.AsReadOnly();

    private readonly List<Mission> _missions = [];
    private readonly List<EmergencyHistory> _history = [];

    private Emergency() { }

    public Emergency(
        string citizenId,
        int emergencyTypeId,
        string? description,
        double latitude,
        double longitude,
        EmergencyPriority priority,
        DateTime responseDeadline)
    {
        if (string.IsNullOrWhiteSpace(citizenId))
            throw new ArgumentException("CitizenId cannot be empty.", nameof(citizenId));

        CitizenId = citizenId;
        EmergencyTypeId = emergencyTypeId;
        Description = description;
        Latitude = latitude;
        Longitude = longitude;
        Priority = priority;
        Status = EmergencyStatus.Created;
        CreatedAt = DateTime.UtcNow;
        ResponseDeadline = responseDeadline;
        TrackingNumber = GenerateTrackingNumber();
    }

    public void TransitionTo(EmergencyStatus newStatus)
    {
        EnsureModifiable();

        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
            throw new InvalidStatusTransitionException(Status, newStatus);

        if (newStatus == EmergencyStatus.Resolved)
            ResolvedAt = DateTime.UtcNow;

        Status = newStatus;
    }

    public void Reassign()
    {
        EnsureModifiable();

        if (!ReassignableStatuses.Contains(Status))
            throw new DomainException($"Emergency in status '{Status}' cannot be reassigned. Reassignment is only allowed for Dispatched, Accepted, or Escalated emergencies.");

        Status = EmergencyStatus.Dispatched;
    }

    public void Escalate() => TransitionTo(EmergencyStatus.Escalated);

    public void AddHistory(EmergencyHistory history) => _history.Add(history);

    public void Cancel()
    {
        EnsureModifiable();

        if (!CancellableStatuses.Contains(Status))
            throw new DomainException($"Emergency in status '{Status}' cannot be cancelled. Resolved, Arrived, and Closed emergencies cannot be cancelled.");

        Status = EmergencyStatus.Cancelled;
    }

    public void Close()
    {
        if (Status != EmergencyStatus.Resolved)
            throw new DomainException($"Emergency in status '{Status}' cannot be closed. Emergency must be in 'Resolved' status.");

        Status = EmergencyStatus.Closed;
    }

    public void Resolve()
    {
        EnsureModifiable();

        if (Status != EmergencyStatus.Arrived)
            throw new DomainException($"Emergency in status '{Status}' cannot be resolved. Teams must have arrived on scene.");

        ResolvedAt = DateTime.UtcNow;
        Status = EmergencyStatus.Resolved;
    }

    public void UpdateLocation(double latitude, double longitude)
    {
        EnsureModifiable();
        Latitude = latitude;
        Longitude = longitude;
    }

    public void UpdatePriority(EmergencyPriority priority)
    {
        EnsureModifiable();
        Priority = priority;
    }

    private void EnsureModifiable()
    {
        if (Status is EmergencyStatus.Resolved or EmergencyStatus.Closed or EmergencyStatus.Cancelled)
            throw new DomainException($"Emergency in status '{Status}' cannot be modified.");
    }

    private static string GenerateTrackingNumber()
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmm");
        return $"RSQ-{timestamp}-{Guid.NewGuid():N}";
    }
}
