using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ResQ.Application.Common.Interfaces;
using ResQ.Domain.Entities;
using ResQ.Domain.Enums;
using ResQ.Domain.Exceptions;

namespace ResQ.Application.Features.Dispatch.Commands;

public enum MissionAction { Assign, Reassign, Accept, Reject, Start, Arrive, Resolve, Close, Cancel }
public sealed record MissionWorkflowCommand(MissionAction Action, int EmergencyId, int? TeamId, string ActorId, int? ActorTeamId, bool IsCitizen, bool IsTeamMember, string? Notes) : IRequest;

public sealed class MissionWorkflowValidator : AbstractValidator<MissionWorkflowCommand>
{
    public MissionWorkflowValidator()
    {
        RuleFor(x => x.EmergencyId).GreaterThan(0);
        RuleFor(x => x.TeamId).NotNull().GreaterThan(0).When(x => x.Action is MissionAction.Assign or MissionAction.Reassign);
        RuleFor(x => x.ActorId).NotEmpty();
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class MissionWorkflowHandler(IApplicationDbContext db) : IRequestHandler<MissionWorkflowCommand>
{
    private static readonly MissionStatus[] ActiveStatuses = [MissionStatus.Assigned, MissionStatus.Accepted, MissionStatus.EnRoute, MissionStatus.OnScene];

    public async Task Handle(MissionWorkflowCommand r, CancellationToken ct)
    {
        var emergency = await db.Emergencies.Include(e => e.EmergencyType).Include(e => e.Missions)
            .FirstOrDefaultAsync(e => e.Id == r.EmergencyId, ct) ?? throw new KeyNotFoundException("Emergency not found.");
        var mission = emergency.Missions.Where(m => ActiveStatuses.Contains(m.Status)).OrderByDescending(m => m.AssignedAt).FirstOrDefault();
        if (r.Action == MissionAction.Cancel && r.IsCitizen && emergency.CitizenId != r.ActorId) throw new UnauthorizedAccessException("You may only cancel your own emergency.");
        var oldStatus = emergency.Status;
        var actionName = r.Action.ToString();

        if (r.Action is MissionAction.Assign or MissionAction.Reassign)
        {
            if (r.Action == MissionAction.Assign && mission != null) throw new DomainException("Emergency already has an active mission; use reassignment.");
            if (r.Action == MissionAction.Reassign && mission == null) throw new DomainException("Emergency has no active mission to reassign.");
            var team = await db.ResponseTeams.FirstOrDefaultAsync(t => t.Id == r.TeamId, ct) ?? throw new KeyNotFoundException("Response team not found.");
            if (team.TeamType != emergency.EmergencyType.RequiredTeamType) throw new DomainException("Team specialization does not match the emergency type.");
            if (team.Status != TeamStatus.Available || await db.Missions.AnyAsync(m => m.ResponseTeamId == team.Id && ActiveStatuses.Contains(m.Status), ct))
                throw new DomainException("Team is unavailable or already has an active mission.");
            if (mission != null)
            {
                var previous = await db.ResponseTeams.FirstAsync(t => t.Id == mission.ResponseTeamId, ct);
                if (mission.Status is MissionStatus.EnRoute or MissionStatus.OnScene)
                    throw new DomainException("A mission cannot be reassigned after the team starts responding.");
                mission.Abort(r.Notes);
                if (!await db.Missions.AnyAsync(m => m.Id != mission.Id && m.ResponseTeamId == previous.Id && ActiveStatuses.Contains(m.Status), ct)) previous.MarkAvailable();
                emergency.Reassign();
            }
            else emergency.TransitionTo(EmergencyStatus.Dispatched);
            team.MarkBusy();
            db.Missions.Add(new Mission(emergency.Id, team.Id, r.Notes));
        }
        else
        {
            if (r.Action is MissionAction.Resolve or MissionAction.Close or MissionAction.Cancel)
            {
                if (r.IsTeamMember && (mission == null || r.ActorTeamId != mission.ResponseTeamId)) throw new UnauthorizedAccessException("You are not assigned to this mission.");
                if (r.Action == MissionAction.Resolve) { emergency.Resolve(); mission?.Complete(r.Notes); }
                else if (r.Action == MissionAction.Close) emergency.Close();
                else { emergency.Cancel(); mission?.Abort(r.Notes); }
                if (mission != null)
                {
                    var team = await db.ResponseTeams.FirstAsync(t => t.Id == mission.ResponseTeamId, ct);
                    if (!await db.Missions.AnyAsync(m => m.Id != mission.Id && m.ResponseTeamId == team.Id && ActiveStatuses.Contains(m.Status), ct)) team.MarkAvailable();
                }
            }
            else
            {
                if (mission == null) throw new DomainException("Emergency has no active mission.");
                if (r.ActorTeamId != mission.ResponseTeamId) throw new UnauthorizedAccessException("You are not assigned to this mission.");
                switch (r.Action)
                {
                    case MissionAction.Accept: mission.Accept(); emergency.TransitionTo(EmergencyStatus.Accepted); break;
                    case MissionAction.Reject:
                        mission.Abort(r.Notes);
                        emergency.Cancel();
                        var rejectedTeam = await db.ResponseTeams.FirstAsync(t => t.Id == mission.ResponseTeamId, ct);
                        if (!await db.Missions.AnyAsync(m => m.Id != mission.Id && m.ResponseTeamId == rejectedTeam.Id && ActiveStatuses.Contains(m.Status), ct)) rejectedTeam.MarkAvailable();
                        break;
                    case MissionAction.Start: mission.MarkEnRoute(); emergency.TransitionTo(EmergencyStatus.OnTheWay); break;
                    case MissionAction.Arrive: mission.MarkOnScene(); emergency.TransitionTo(EmergencyStatus.Arrived); break;
                    default: throw new DomainException("Unsupported workflow action.");
                }
            }
        }

        emergency.AddHistory(new EmergencyHistory(emergency.Id, actionName, r.ActorId, oldStatus, emergency.Status, r.Notes));
        await db.SaveChangesAsync(ct);
    }
}
