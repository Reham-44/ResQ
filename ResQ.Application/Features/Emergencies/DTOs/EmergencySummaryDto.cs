namespace ResQ.Application.Features.Emergencies.DTOs;

/// <summary>Emergency fields returned in emergency list results.</summary>
/// <param name="Id">Database identifier.</param>
/// <param name="TrackingNumber">Public tracking number.</param>
/// <param name="CitizenId">Identifier of the citizen who created the emergency.</param>
/// <param name="EmergencyTypeId">Emergency type identifier.</param>
/// <param name="EmergencyTypeName">Emergency type name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Latitude">Emergency latitude.</param>
/// <param name="Longitude">Emergency longitude.</param>
/// <param name="Priority">Priority name.</param>
/// <param name="Status">Current emergency status name.</param>
/// <param name="CreatedAt">Creation time in UTC.</param>
/// <param name="ResponseDeadline">SLA response deadline in UTC.</param>
/// <param name="ResolvedAt">Resolution time in UTC, when resolved.</param>
public record EmergencySummaryDto(
    int Id,
    string TrackingNumber,
    string CitizenId,
    int EmergencyTypeId,
    string EmergencyTypeName,
    string? Description,
    double Latitude,
    double Longitude,
    string Priority,
    string Status,
    DateTime CreatedAt,
    DateTime ResponseDeadline,
    DateTime? ResolvedAt);
