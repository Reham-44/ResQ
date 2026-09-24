using MediatR;
using Microsoft.EntityFrameworkCore;
using ResQ.Application.Common.Interfaces;
using ResQ.Domain.Enums;

namespace ResQ.Application.Features.Dispatch.Queries;

public sealed record GetAvailableTeamsQuery(int EmergencyId) : IRequest<IReadOnlyList<TeamDto>>;
public sealed record GetMissionHistoryQuery(int EmergencyId) : IRequest<IReadOnlyList<HistoryDto>>;
public sealed record GetMissionsQuery(int? TeamId) : IRequest<IReadOnlyList<MissionDto>>;
public sealed record TeamDto(int Id, string Name, string Type, string Status);
public sealed record HistoryDto(string Action, string PerformedBy, string? OldStatus, string? NewStatus, DateTime CreatedAt, string? Notes);
public sealed record MissionDto(int Id, int EmergencyId, int TeamId, string TeamName, string Status, DateTime AssignedAt, string? Notes);

public sealed class DispatchQueryHandler(IApplicationDbContext db) :
    IRequestHandler<GetAvailableTeamsQuery, IReadOnlyList<TeamDto>>,
    IRequestHandler<GetMissionHistoryQuery, IReadOnlyList<HistoryDto>>,
    IRequestHandler<GetMissionsQuery, IReadOnlyList<MissionDto>>
{
    private static readonly MissionStatus[] Active = [MissionStatus.Assigned, MissionStatus.Accepted, MissionStatus.EnRoute, MissionStatus.OnScene];
    public async Task<IReadOnlyList<TeamDto>> Handle(GetAvailableTeamsQuery r, CancellationToken ct)
    {
        var type = await db.Emergencies.Where(e => e.Id == r.EmergencyId).Select(e => (TeamType?)e.EmergencyType.RequiredTeamType).FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Emergency not found.");
        return await db.ResponseTeams.AsNoTracking().Where(t => t.TeamType == type && t.Status == TeamStatus.Available && !db.Missions.Any(m => m.ResponseTeamId == t.Id && Active.Contains(m.Status)))
            .OrderBy(t => t.Name).Select(t => new TeamDto(t.Id, t.Name, t.TeamType.ToString(), t.Status.ToString())).ToListAsync(ct);
    }
    public async Task<IReadOnlyList<HistoryDto>> Handle(GetMissionHistoryQuery r, CancellationToken ct)
    {
        if (!await db.Emergencies.AnyAsync(e => e.Id == r.EmergencyId, ct))
            throw new KeyNotFoundException("Emergency not found.");

        return await db.EmergencyHistories.AsNoTracking()
            .Where(h => h.EmergencyId == r.EmergencyId)
            .OrderByDescending(h => h.CreatedAt)
            .Select(h => new HistoryDto(h.Action, h.PerformedBy, h.OldStatus.HasValue ? h.OldStatus.ToString() : null, h.NewStatus.HasValue ? h.NewStatus.ToString() : null, h.CreatedAt, h.Notes))
            .ToListAsync(ct);
    }
    public async Task<IReadOnlyList<MissionDto>> Handle(GetMissionsQuery r, CancellationToken ct) => await db.Missions.AsNoTracking().Where(m => r.TeamId == null || m.ResponseTeamId == r.TeamId).OrderByDescending(m => m.AssignedAt).Select(m => new MissionDto(m.Id, m.EmergencyId, m.ResponseTeamId, m.ResponseTeam.Name, m.Status.ToString(), m.AssignedAt, m.Notes)).ToListAsync(ct);
}
