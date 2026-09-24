using ResQ.Application.Features.Emergencies;
using ResQ.Domain.Entities;
using ResQ.Domain.Enums;
using ResQ.Domain.Exceptions;

namespace ResQ.Tests;

public sealed class DomainWorkflowTests
{
    [Fact]
    public void Emergency_accepts_valid_dispatch_flow_and_rejects_invalid_transition()
    {
        var emergency = NewEmergency();
        emergency.TransitionTo(EmergencyStatus.Dispatched);
        emergency.TransitionTo(EmergencyStatus.Accepted);
        emergency.TransitionTo(EmergencyStatus.OnTheWay);
        emergency.TransitionTo(EmergencyStatus.Arrived);
        Assert.Throws<InvalidStatusTransitionException>(() => emergency.TransitionTo(EmergencyStatus.Dispatched));
        emergency.Resolve();
        emergency.Close();

        Assert.Equal(EmergencyStatus.Closed, emergency.Status);
    }

    [Theory]
    [InlineData(EmergencyStatus.Resolved)]
    [InlineData(EmergencyStatus.Closed)]
    [InlineData(EmergencyStatus.Cancelled)]
    public void Terminal_emergency_cannot_be_escalated(EmergencyStatus status)
    {
        var emergency = NewEmergency();
        if (status == EmergencyStatus.Cancelled)
            emergency.Cancel();
        else
        {
            emergency.TransitionTo(EmergencyStatus.Dispatched);
            emergency.TransitionTo(EmergencyStatus.Accepted);
            emergency.TransitionTo(EmergencyStatus.OnTheWay);
            emergency.TransitionTo(EmergencyStatus.Arrived);
            emergency.Resolve();
            if (status == EmergencyStatus.Closed) emergency.Close();
        }

        Assert.Throws<DomainException>(emergency.Escalate);
    }

    [Fact]
    public void Mission_follows_assignment_accept_start_arrive_complete_flow()
    {
        var mission = new Mission(10, 20);
        Assert.Equal(MissionStatus.Assigned, mission.Status);
        mission.Accept();
        Assert.Equal(MissionStatus.Accepted, mission.Status);
        mission.MarkEnRoute();
        Assert.Equal(MissionStatus.EnRoute, mission.Status);
        mission.MarkOnScene();
        Assert.Equal(MissionStatus.OnScene, mission.Status);
        mission.Complete();
        Assert.Equal(MissionStatus.Completed, mission.Status);
        Assert.Throws<DomainException>(mission.MarkOnScene);
    }

    [Fact]
    public void Assigned_mission_can_be_rejected_but_accepted_mission_cannot()
    {
        var mission = new Mission(1, 2);
        mission.Reject("Not available");
        Assert.Equal(MissionStatus.Aborted, mission.Status);

        var accepted = new Mission(1, 2);
        accepted.Accept();
        Assert.Throws<DomainException>(() => accepted.Reject());
    }

    [Theory]
    [InlineData(EmergencyPriority.Low, 120)]
    [InlineData(EmergencyPriority.Medium, 60)]
    [InlineData(EmergencyPriority.High, 30)]
    [InlineData(EmergencyPriority.Critical, 10)]
    public void Sla_deadline_uses_priority_response_window(EmergencyPriority priority, int minutes)
    {
        var createdAt = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(createdAt.AddMinutes(minutes), EmergencySlaPolicy.CalculateDeadline(priority, createdAt));
    }

    private static Emergency NewEmergency() => new("citizen-1", 1, null, 30, 31, EmergencyPriority.Medium, DateTime.UtcNow.AddHours(1));
}
