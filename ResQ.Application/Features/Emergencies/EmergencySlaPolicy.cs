using ResQ.Domain.Enums;

namespace ResQ.Application.Features.Emergencies;

public static class EmergencySlaPolicy
{
    public static TimeSpan GetResponseWindow(EmergencyPriority priority) => priority switch
    {
        EmergencyPriority.Low => TimeSpan.FromMinutes(120),
        EmergencyPriority.Medium => TimeSpan.FromMinutes(60),
        EmergencyPriority.High => TimeSpan.FromMinutes(30),
        EmergencyPriority.Critical => TimeSpan.FromMinutes(10),
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown emergency priority.")
    };

    public static DateTime CalculateDeadline(EmergencyPriority priority, DateTime createdAt) => createdAt.Add(GetResponseWindow(priority));
}
