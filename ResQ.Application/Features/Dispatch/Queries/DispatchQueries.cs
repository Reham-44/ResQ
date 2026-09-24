using MediatR;
using Microsoft.EntityFrameworkCore;
using ResQ.Application.Common.Interfaces;
using ResQ.Domain.Enums;

namespace ResQ.Application.Features.Dispatch.Queries;

public sealed record GetAvailableTeamsQuery(int EmergencyId) : IRequest<IReadOnlyList<TeamDto>>;
public sealed record GetMissionHistoryQuery(int EmergencyId) : IRequest<IReadOnlyList<HistoryDto>>;
public sealed record GetMissionsQuery(int? TeamId) : IRequest<IReadOnlyList<MissionDto>>;

/// <summary>Eligible response team returned for dispatch.</summary>
/// <param name="Id">Team identifier.</param>
/// <param name="Name">Team name.</param>
/// <param name="Type">Team specialization.</param>
/// <param name="Status">Current team status.</param>
public sealed record TeamDto(int Id, string Name, string Type, string Status);

/// <summary>Recorded emergency workflow or escalation action.</summary>
/// <param name="Action">Action name.</param>
/// <param name="PerformedBy">User identifier or system actor that performed the action.</param>
/// <param name="OldStatus">Emergency status before the action, when recorded.</param>
/// <param name="NewStatus">Emergency status after the action, when recorded.</param>
/// <param name="CreatedAt">History entry time in UTC.</param>
/// <param name="Notes">Optional action notes.</param>
public sealed record HistoryDto(string Action, string PerformedBy, string? OldStatus, string? NewStatus, DateTime CreatedAt, string? Notes);

/// <summary>Mission summary returned by dispatch queries.</summary>
/// <param name="Id">Mission identifier.</param>
/// <param name="EmergencyId">Associated emergency identifier.</param>
/// <param name="TeamId">Assigned response team identifier.</param>
/// <param name="TeamName">Assigned response team name.</param>
/// <param name="Status">Current mission status.</param>
/// <param name="AssignedAt">Assignment time in UTC.</param>
/// <param name="Notes">Optional mission notes.</param>
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
