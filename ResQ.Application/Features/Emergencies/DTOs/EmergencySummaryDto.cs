namespace ResQ.Application.Features.Emergencies.DTOs;

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
