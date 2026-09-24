using System.Text.Json.Serialization;
using MediatR;
using ResQ.Domain.Enums;

namespace ResQ.Application.Features.Emergencies.Commands.CreateEmergency;

/// <summary>Fields accepted to create an emergency. Citizen identity is assigned from authentication.</summary>
public class CreateEmergencyCommand : IRequest<CreateEmergencyResponseDto>
{
    /// <summary>Identifier of an existing emergency type.</summary>
    public int EmergencyTypeId { get; set; }
    /// <summary>Optional description of the emergency.</summary>
    public string? Description { get; set; }
    /// <summary>Latitude in the range -90 through 90.</summary>
    public double Latitude { get; set; }
    /// <summary>Longitude in the range -180 through 180.</summary>
    public double Longitude { get; set; }
    /// <summary>Emergency priority: Low, Medium, High, or Critical.</summary>
    public EmergencyPriority Priority { get; set; }

    /// <summary>Authenticated citizen identifier; ignored in JSON input and set by the API.</summary>
    [JsonIgnore]
    public string CitizenId { get; set; } = string.Empty;
}
