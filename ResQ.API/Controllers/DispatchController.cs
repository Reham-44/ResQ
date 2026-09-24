using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ResQ.Application.Features.Dispatch.Commands;
using ResQ.Application.Features.Dispatch.Queries;
using ResQ.Infrastructure.Identity;

namespace ResQ.API.Controllers;

[ApiController, Route("api")]
[Authorize]
public sealed class DispatchController(ISender sender, UserManager<ApplicationUser> users) : ControllerBase
{
    private string ActorId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? string.Empty;

    private async Task<int?> CurrentTeamId(CancellationToken ct)
    {
        var user = await users.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == ActorId, ct);
        return user?.ResponseTeamId;
    }

    [HttpPost("emergencies/{id:int}/assign"), Authorize(Roles = "Dispatcher,Admin")]
    public async Task<IActionResult> Assign(int id, TeamRequest body, CancellationToken ct) => await Execute(MissionAction.Assign, id, body, ct);
    [HttpPost("emergencies/{id:int}/reassign"), Authorize(Roles = "Dispatcher,Admin")]
    public async Task<IActionResult> Reassign(int id, TeamRequest body, CancellationToken ct) => await Execute(MissionAction.Reassign, id, body, ct);
    [HttpPost("emergencies/{id:int}/accept"), Authorize(Roles = "ResponseTeamMember")]
    public async Task<IActionResult> Accept(int id, NotesRequest? body, CancellationToken ct) => await Execute(MissionAction.Accept, id, body, ct);
    [HttpPost("emergencies/{id:int}/reject"), Authorize(Roles = "ResponseTeamMember")]
    public async Task<IActionResult> Reject(int id, NotesRequest? body, CancellationToken ct) => await Execute(MissionAction.Reject, id, body, ct);
    [HttpPost("emergencies/{id:int}/start"), Authorize(Roles = "ResponseTeamMember")]
    public async Task<IActionResult> Start(int id, NotesRequest? body, CancellationToken ct) => await Execute(MissionAction.Start, id, body, ct);
    [HttpPost("emergencies/{id:int}/arrive"), Authorize(Roles = "ResponseTeamMember")]
    public async Task<IActionResult> Arrive(int id, NotesRequest? body, CancellationToken ct) => await Execute(MissionAction.Arrive, id, body, ct);
    [HttpPost("emergencies/{id:int}/resolve"), Authorize(Roles = "Dispatcher,Admin,ResponseTeamMember")]
    public async Task<IActionResult> Resolve(int id, NotesRequest? body, CancellationToken ct) => await Execute(MissionAction.Resolve, id, body, ct);
    [HttpPost("emergencies/{id:int}/close"), Authorize(Roles = "Dispatcher,Admin")]
    public async Task<IActionResult> Close(int id, NotesRequest? body, CancellationToken ct) => await Execute(MissionAction.Close, id, body, ct);
    [HttpPost("emergencies/{id:int}/cancel"), Authorize(Roles = "Dispatcher,Admin,Citizen")]
    public async Task<IActionResult> Cancel(int id, NotesRequest? body, CancellationToken ct) => await Execute(MissionAction.Cancel, id, body, ct);

    [HttpGet("emergencies/{id:int}/available-teams"), Authorize(Roles = "Dispatcher,Admin")]
    public async Task<IActionResult> AvailableTeams(int id, CancellationToken ct) => Ok(await sender.Send(new GetAvailableTeamsQuery(id), ct));
    [HttpGet("emergencies/{id:int}/history"), Authorize(Roles = "Dispatcher,Admin")]
    public async Task<IActionResult> History(int id, CancellationToken ct)
    {
        try { return Ok(await sender.Send(new GetMissionHistoryQuery(id), ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
    [HttpGet("missions"), Authorize(Roles = "Dispatcher,Admin,ResponseTeamMember")]
    public async Task<IActionResult> Missions(CancellationToken ct)
    {
        var teamId = User.IsInRole("ResponseTeamMember") ? await CurrentTeamId(ct) : null;
        if (User.IsInRole("ResponseTeamMember") && teamId == null) return Forbid();
        return Ok(await sender.Send(new GetMissionsQuery(teamId), ct));
    }

    private async Task<IActionResult> Execute(MissionAction action, int id, NotesRequest? body, CancellationToken ct, TeamRequest? team = null)
    {
        try
        {
            var teamId = User.IsInRole("ResponseTeamMember") ? await CurrentTeamId(ct) : null;
            if (User.IsInRole("ResponseTeamMember") && teamId == null) return Forbid();
            await sender.Send(new MissionWorkflowCommand(action, id, team?.TeamId, ActorId, teamId, User.IsInRole("Citizen"), User.IsInRole("ResponseTeamMember"), body?.Notes), ct);
            return NoContent();
        }
        catch (DbUpdateConcurrencyException) { return Conflict(new { message = "The team or emergency was changed by another request. Refresh and retry." }); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ResQ.Domain.Exceptions.DomainException ex) { return Conflict(new { message = ex.Message }); }
    }

    private Task<IActionResult> Execute(MissionAction action, int id, TeamRequest body, CancellationToken ct) => Execute(action, id, null, ct, body);
    public sealed record TeamRequest(int TeamId);
    public sealed record NotesRequest(string? Notes);
}
