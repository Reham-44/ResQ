using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using ResQ.Application.Common.Interfaces;
using ResQ.Application.Extensions;
using ResQ.Application.Features.Dispatch.Commands;
using ResQ.Application.Features.Dispatch.Queries;
using ResQ.Application.Features.Emergencies.Commands.CreateEmergency;
using ResQ.Application.Features.Emergencies.Commands.EscalateEmergency;
using ResQ.Application.Features.Emergencies.Commands.MonitorOverdueEmergencies;
using ResQ.Domain.Entities;
using ResQ.Domain.Enums;
using ResQ.Domain.Exceptions;

namespace ResQ.Tests;

public sealed class ApplicationWorkflowTests
{
    [Fact]
    public async Task Create_emergency_sets_deadline_and_persists()
    {
        await using var db = TestDatabase.Create();
        var handler = new CreateEmergencyCommandHandler(db);
        var before = DateTime.UtcNow;
        var result = await handler.Handle(new CreateEmergencyCommand
        {
            CitizenId = "citizen-1", EmergencyTypeId = 1, Latitude = 30, Longitude = 31,
            Priority = EmergencyPriority.High
        }, CancellationToken.None);
        var emergency = await db.Emergencies.SingleAsync();

        Assert.Equal(result.Id, emergency.Id);
        Assert.InRange(emergency.ResponseDeadline, before.AddMinutes(30), DateTime.UtcNow.AddMinutes(30));
    }

    [Fact]
    public async Task Escalation_is_idempotent_and_creates_one_history_and_notification()
    {
        await using var db = TestDatabase.Create();
        var emergency = new Emergency("citizen-1", 1, null, 30, 31, EmergencyPriority.High, DateTime.UtcNow.AddMinutes(-1));
        db.Emergencies.Add(emergency);
        await db.SaveChangesAsync();
        var handler = new EscalateEmergencyCommandHandler(db);

        Assert.True(await handler.Handle(new EscalateEmergencyCommand(emergency.Id), CancellationToken.None));
        Assert.False(await handler.Handle(new EscalateEmergencyCommand(emergency.Id), CancellationToken.None));
        Assert.Equal(EmergencyStatus.Escalated, emergency.Status);
        Assert.Single(await db.EmergencyHistories.ToListAsync());
        Assert.Single(await db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task Escalation_ignores_missing_future_and_terminal_emergencies()
    {
        await using var db = TestDatabase.Create();
        var future = new Emergency("citizen-1", 1, null, 30, 31, EmergencyPriority.Low, DateTime.UtcNow.AddHours(1));
        var cancelled = new Emergency("citizen-2", 1, null, 30, 31, EmergencyPriority.Low, DateTime.UtcNow.AddHours(-1));
        cancelled.Cancel();
        db.Emergencies.AddRange(future, cancelled);
        await db.SaveChangesAsync();
        var handler = new EscalateEmergencyCommandHandler(db);

        Assert.False(await handler.Handle(new EscalateEmergencyCommand(999), CancellationToken.None));
        Assert.False(await handler.Handle(new EscalateEmergencyCommand(future.Id), CancellationToken.None));
        Assert.False(await handler.Handle(new EscalateEmergencyCommand(cancelled.Id), CancellationToken.None));
        Assert.Empty(await db.EmergencyHistories.ToListAsync());
        Assert.Empty(await db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task Monitor_escalates_overdue_emergencies_once_and_ignores_cancelled_emergencies()
    {
        await using var db = TestDatabase.Create();
        var overdue = new Emergency("citizen-1", 1, null, 30, 31, EmergencyPriority.Critical, DateTime.UtcNow.AddMinutes(-1));
        var cancelled = new Emergency("citizen-2", 1, null, 30, 31, EmergencyPriority.Critical, DateTime.UtcNow.AddMinutes(-1));
        cancelled.Cancel();
        db.Emergencies.AddRange(overdue, cancelled);
        await db.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IApplicationDbContext>(db);
        using var provider = services.BuildServiceProvider();
        var handler = new MonitorOverdueEmergenciesCommandHandler(db, provider.GetRequiredService<ISender>());

        Assert.Equal(1, await handler.Handle(new MonitorOverdueEmergenciesCommand(), CancellationToken.None));
        Assert.Equal(0, await handler.Handle(new MonitorOverdueEmergenciesCommand(), CancellationToken.None));
        Assert.Equal(EmergencyStatus.Escalated, overdue.Status);
        Assert.Equal(EmergencyStatus.Cancelled, cancelled.Status);
        Assert.Single(await db.EmergencyHistories.ToListAsync());
        Assert.Single(await db.Notifications.ToListAsync());
    }

    [Fact]
    public async Task Assign_rejects_team_with_wrong_specialization()
    {
        await using var db = TestDatabase.Create();
        var (emergency, _) = await SeedEmergencyAndTeam(db, TeamType.Fire);
        var handler = new MissionWorkflowHandler(db);

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(Command(MissionAction.Assign, emergency.Id, 1), CancellationToken.None));
        Assert.Empty(await db.Missions.ToListAsync());
    }

    [Fact]
    public async Task Assign_prevents_duplicate_active_missions_and_unavailable_teams()
    {
        await using var db = TestDatabase.Create();
        var (emergency, _) = await SeedEmergencyAndTeam(db, TeamType.Medical);
        var handler = new MissionWorkflowHandler(db);
        await handler.Handle(Command(MissionAction.Assign, emergency.Id, 1), CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(Command(MissionAction.Assign, emergency.Id, 1), CancellationToken.None));
        Assert.Single(await db.Missions.ToListAsync());
    }

    [Fact]
    public async Task Application_workflow_completes_assign_accept_start_arrive_resolve_close()
    {
        await using var db = TestDatabase.Create();
        var (emergency, team) = await SeedEmergencyAndTeam(db, TeamType.Medical);
        var handler = new MissionWorkflowHandler(db);

        await handler.Handle(Command(MissionAction.Assign, emergency.Id, team.Id), CancellationToken.None);
        var memberCommand = (MissionAction action) =>
            new MissionWorkflowCommand(action, emergency.Id, null, "member-1", team.Id, false, true, null);
        await handler.Handle(memberCommand(MissionAction.Accept), CancellationToken.None);
        await handler.Handle(memberCommand(MissionAction.Start), CancellationToken.None);
        await handler.Handle(memberCommand(MissionAction.Arrive), CancellationToken.None);
        await handler.Handle(memberCommand(MissionAction.Resolve), CancellationToken.None);
        await handler.Handle(Command(MissionAction.Close, emergency.Id, null), CancellationToken.None);

        Assert.Equal(EmergencyStatus.Closed, emergency.Status);
        Assert.Equal(MissionStatus.Completed, await db.Missions.Select(m => m.Status).SingleAsync());
        Assert.Equal(6, await db.EmergencyHistories.CountAsync());
    }

    [Fact]
    public async Task Reassign_aborts_old_mission_without_changing_its_team_association()
    {
        await using var db = TestDatabase.Create();
        var (emergency, oldTeam) = await SeedEmergencyAndTeam(db, TeamType.Medical);
        var newTeam = new ResponseTeam("Team 2", TeamType.Medical, 30, 31);
        db.ResponseTeams.Add(newTeam);
        await db.SaveChangesAsync();
        var handler = new MissionWorkflowHandler(db);
        await handler.Handle(Command(MissionAction.Assign, emergency.Id, oldTeam.Id), CancellationToken.None);
        await handler.Handle(Command(MissionAction.Reassign, emergency.Id, newTeam.Id), CancellationToken.None);

        var missions = await db.Missions.OrderBy(m => m.Id).ToListAsync();
        Assert.Equal(2, missions.Count);
        Assert.Equal(oldTeam.Id, missions[0].ResponseTeamId);
        Assert.Equal(MissionStatus.Aborted, missions[0].Status);
        Assert.Equal(newTeam.Id, missions[1].ResponseTeamId);
        Assert.Equal(MissionStatus.Assigned, missions[1].Status);
    }

    [Fact]
    public async Task Assign_rejects_a_team_marked_offline()
    {
        await using var db = TestDatabase.Create();
        var (emergency, team) = await SeedEmergencyAndTeam(db, TeamType.Medical);
        team.UpdateStatus(TeamStatus.Offline);

        await Assert.ThrowsAsync<DomainException>(() => new MissionWorkflowHandler(db)
            .Handle(Command(MissionAction.Assign, emergency.Id, team.Id), CancellationToken.None));
        Assert.Empty(await db.Missions.ToListAsync());
    }

    [Fact]
    public async Task Team_member_cannot_operate_on_another_teams_mission()
    {
        await using var db = TestDatabase.Create();
        var (emergency, _) = await SeedEmergencyAndTeam(db, TeamType.Medical);
        db.ResponseTeams.Add(new ResponseTeam("Other team", TeamType.Medical, 30, 31));
        await db.SaveChangesAsync();
        var handler = new MissionWorkflowHandler(db);
        await handler.Handle(Command(MissionAction.Assign, emergency.Id, 1), CancellationToken.None);

        var unauthorized = new MissionWorkflowCommand(MissionAction.Accept, emergency.Id, null, "member-2", 2, false, true, null);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => handler.Handle(unauthorized, CancellationToken.None));
    }

    [Fact]
    public async Task Citizen_cannot_cancel_another_citizens_emergency()
    {
        await using var db = TestDatabase.Create();
        db.EmergencyTypes.Add(new EmergencyType("Medical incident", TeamType.Medical));
        var emergency = new Emergency("owner", 1, null, 30, 31, EmergencyPriority.Low, DateTime.UtcNow.AddHours(1));
        db.Emergencies.Add(emergency);
        await db.SaveChangesAsync();
        var command = new MissionWorkflowCommand(MissionAction.Cancel, emergency.Id, null, "intruder", null, true, false, null);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new MissionWorkflowHandler(db).Handle(command, CancellationToken.None));
        Assert.Equal(EmergencyStatus.Created, emergency.Status);
    }

    [Fact]
    public async Task Resolve_requires_an_active_mission()
    {
        await using var db = TestDatabase.Create();
        var emergency = new Emergency("citizen-1", 1, null, 30, 31, EmergencyPriority.Low, DateTime.UtcNow.AddHours(1));
        db.Emergencies.Add(emergency);
        await db.SaveChangesAsync();
        var command = new MissionWorkflowCommand(MissionAction.Resolve, emergency.Id, null, "dispatcher", null, false, false, null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => new MissionWorkflowHandler(db).Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Missing_emergency_and_missing_team_are_reported()
    {
        await using var db = TestDatabase.Create();
        var handler = new MissionWorkflowHandler(db);
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(Command(MissionAction.Assign, 999, 10), CancellationToken.None));

        var emergency = new Emergency("citizen-1", 1, null, 30, 31, EmergencyPriority.Low, DateTime.UtcNow.AddHours(1));
        db.Emergencies.Add(emergency);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => handler.Handle(Command(MissionAction.Assign, emergency.Id, 10), CancellationToken.None));
    }

    [Fact]
    public async Task Mission_history_reports_a_missing_emergency()
    {
        await using var db = TestDatabase.Create();
        await Assert.ThrowsAsync<KeyNotFoundException>(() => new DispatchQueryHandler(db)
            .Handle(new GetMissionHistoryQuery(404), CancellationToken.None));
    }

    private static MissionWorkflowCommand Command(MissionAction action, int emergencyId, int? teamId) =>
        new(action, emergencyId, teamId, "dispatcher", null, false, false, null);

    private static async Task<(Emergency emergency, ResponseTeam team)> SeedEmergencyAndTeam(ResQ.Infrastructure.Persistence.ResQDbContext db, TeamType teamType)
    {
        db.EmergencyTypes.Add(new EmergencyType("Medical incident", TeamType.Medical));
        var emergency = new Emergency("citizen-1", 1, null, 30, 31, EmergencyPriority.Medium, DateTime.UtcNow.AddHours(1));
        var team = new ResponseTeam("Team 1", teamType, 30, 31);
        db.Emergencies.Add(emergency);
        db.ResponseTeams.Add(team);
        await db.SaveChangesAsync();
        return (emergency, team);
    }
}
