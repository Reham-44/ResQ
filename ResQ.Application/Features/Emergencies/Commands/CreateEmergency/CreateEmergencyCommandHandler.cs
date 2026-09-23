using MediatR;
using ResQ.Application.Common.Interfaces;
using ResQ.Domain.Entities;
using ResQ.Domain.Enums;

namespace ResQ.Application.Features.Emergencies.Commands.CreateEmergency;

public class CreateEmergencyCommandHandler : IRequestHandler<CreateEmergencyCommand, CreateEmergencyResponseDto>
{
    private readonly IApplicationDbContext _context;

    public CreateEmergencyCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CreateEmergencyResponseDto> Handle(CreateEmergencyCommand request, CancellationToken cancellationToken)
    {
        var deadlineMinutes = request.Priority switch
        {
            EmergencyPriority.Low => 120,
            EmergencyPriority.Medium => 60,
            EmergencyPriority.High => 30,
            EmergencyPriority.Critical => 10,
            _ => 60
        };

        var responseDeadline = DateTime.UtcNow.AddMinutes(deadlineMinutes);

        var emergency = new Emergency(
            citizenId: request.CitizenId,
            emergencyTypeId: request.EmergencyTypeId,
            description: request.Description,
            latitude: request.Latitude,
            longitude: request.Longitude,
            priority: request.Priority,
            responseDeadline: responseDeadline);

        _context.Emergencies.Add(emergency);
        await _context.SaveChangesAsync(cancellationToken);

        return new CreateEmergencyResponseDto(emergency.Id, emergency.TrackingNumber);
    }
}
