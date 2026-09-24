namespace ResQ.Application.Features.Emergencies.Commands.CreateEmergency;

/// <summary>Identifier and tracking number returned for a newly created emergency.</summary>
/// <param name="Id">Database identifier of the emergency.</param>
/// <param name="TrackingNumber">Public tracking number assigned to the emergency.</param>
public record CreateEmergencyResponseDto(int Id, string TrackingNumber);
