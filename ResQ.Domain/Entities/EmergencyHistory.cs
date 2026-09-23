using ResQ.Domain.Enums;

namespace ResQ.Domain.Entities;

public class EmergencyHistory
{
    public int Id { get; private set; }
    public int EmergencyId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public EmergencyStatus? OldStatus { get; private set; }
    public EmergencyStatus? NewStatus { get; private set; }
    public string PerformedBy { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public string? Notes { get; private set; }

    // Navigation
    public Emergency Emergency { get; private set; } = null!;

    private EmergencyHistory() { }

    public EmergencyHistory(
        int emergencyId,
        string action,
        string performedBy,
        EmergencyStatus? oldStatus = null,
        EmergencyStatus? newStatus = null,
        string? notes = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action cannot be empty.", nameof(action));
        if (string.IsNullOrWhiteSpace(performedBy))
            throw new ArgumentException("PerformedBy cannot be empty.", nameof(performedBy));

        EmergencyId = emergencyId;
        Action = action;
        PerformedBy = performedBy;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
    }
}
