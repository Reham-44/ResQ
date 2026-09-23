using ResQ.Domain.Enums;
using ResQ.Domain.Exceptions;

namespace ResQ.Domain.Entities;

public class Mission
{
    public int Id { get; private set; }
    public int EmergencyId { get; private set; }
    public int ResponseTeamId { get; private set; }
    public MissionStatus Status { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public DateTime? EnRouteAt { get; private set; }
    public DateTime? OnSceneAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? Notes { get; private set; }

    // Navigation
    public Emergency Emergency { get; private set; } = null!;
    public ResponseTeam ResponseTeam { get; private set; } = null!;

    private Mission() { }

    public Mission(int emergencyId, int responseTeamId, string? notes = null)
    {
        EmergencyId = emergencyId;
        ResponseTeamId = responseTeamId;
        Status = MissionStatus.Assigned;
        AssignedAt = DateTime.UtcNow;
        Notes = notes;
    }

    public void Accept()
    {
        EnsureActive();
        if (Status != MissionStatus.Assigned)
            throw new DomainException($"Cannot accept mission in status '{Status}'. Mission must be 'Assigned'.");

        Status = MissionStatus.Accepted;
    }

    public void MarkEnRoute()
    {
        EnsureActive();
        if (Status != MissionStatus.Accepted && Status != MissionStatus.Assigned)
            throw new DomainException($"Cannot start mission in status '{Status}'. Mission must be 'Accepted' or 'Assigned'.");

        Status = MissionStatus.EnRoute;
        EnRouteAt = DateTime.UtcNow;
    }

    public void MarkOnScene()
    {
        EnsureActive();
        if (Status != MissionStatus.EnRoute)
            throw new DomainException($"Cannot mark on scene from status '{Status}'. Mission must be 'EnRoute'.");

        Status = MissionStatus.OnScene;
        OnSceneAt = DateTime.UtcNow;
    }

    public void Complete(string? notes = null)
    {
        EnsureActive();
        if (Status != MissionStatus.OnScene)
            throw new DomainException($"Cannot complete mission from status '{Status}'. Mission must be 'OnScene'.");

        Status = MissionStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(notes))
            Notes = notes;
    }

    public void Abort(string? notes = null)
    {
        if (Status is MissionStatus.Completed or MissionStatus.Aborted)
            throw new DomainException($"Mission in status '{Status}' cannot be aborted.");

        Status = MissionStatus.Aborted;
        CompletedAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(notes))
            Notes = notes;
    }

    private void EnsureActive()
    {
        if (Status is MissionStatus.Completed or MissionStatus.Aborted)
            throw new DomainException($"Mission in status '{Status}' is no longer active.");
    }
}
