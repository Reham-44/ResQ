using MediatR;
using ResQ.Application.Common.Interfaces;
using ResQ.Application.Features.Emergencies;
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
        var responseDeadline = EmergencySlaPolicy.CalculateDeadline(request.Priority, DateTime.UtcNow);

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
