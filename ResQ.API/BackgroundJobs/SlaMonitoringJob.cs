using MediatR;
using ResQ.Application.Features.Emergencies.Commands.MonitorOverdueEmergencies;

namespace ResQ.API.BackgroundJobs;

public sealed class SlaMonitoringJob(ISender sender)
{
    public async Task RunAsync() => await sender.Send(new MonitorOverdueEmergenciesCommand());
}
