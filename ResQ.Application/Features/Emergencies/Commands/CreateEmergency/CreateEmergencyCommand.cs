using System.Text.Json.Serialization;
using MediatR;
using ResQ.Domain.Enums;

namespace ResQ.Application.Features.Emergencies.Commands.CreateEmergency;

public class CreateEmergencyCommand : IRequest<CreateEmergencyResponseDto>
{
    public int EmergencyTypeId { get; set; }
    public string? Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public EmergencyPriority Priority { get; set; }

    [JsonIgnore]
    public string CitizenId { get; set; } = string.Empty;
}
