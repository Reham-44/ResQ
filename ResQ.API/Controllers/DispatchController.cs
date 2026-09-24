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

/// <summary>Dispatches response teams and exposes mission, team, and emergency-history queries.</summary>
/// <remarks>All actions require a JWT. Team actions use the response-team association currently stored for the user.</remarks>
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

    /// <summary>Assigns an available team with the required specialization to an emergency.</summary>
    /// <param name="id">The emergency ID.</param>
    /// <param name="body">The ID of the team to assign.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>No content when the assignment succeeds.</returns>
    /// <response code="204">The team was assigned.</response>
    /// <response code="400">The emergency ID or team ID is invalid.</response>
    /// <response code="401">Authentication failed.</response>
    /// <response code="403">The caller is not a Dispatcher or Admin.</response>
    /// <response code="404">The emergency or team was not found.</response>
    /// <response code="409">The emergency/team state, specialization, availability, or concurrency check prevented assignment.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost("emergencies/{id:int}/assign"), Authorize(Roles = "Dispatcher,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Assign(int id, TeamRequest body, CancellationToken ct) => await Execute(MissionAction.Assign, id, body, ct);

    /// <summary>Reassigns an eligible emergency to another available, appropriately specialized team.</summary>
    /// <param name="id">The emergency ID.</param>
    /// <param name="body">The ID of the new team.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>No content when reassignment succeeds.</returns>
    /// <response code="204">The new team was assigned and the previous mission was aborted.</response>
    /// <response code="400">The emergency ID or team ID is invalid.</response>
    /// <response code="401">Authentication failed.</response>
    /// <response code="403">The caller is not a Dispatcher or Admin.</response>
    /// <response code="404">The emergency or team was not found.</response>
    /// <response code="409">No eligible mission exists, the mission has started responding, the new team is ineligible, or concurrency prevented the change.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost("emergencies/{id:int}/reassign"), Authorize(Roles = "Dispatcher,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Reassign(int id, TeamRequest body, CancellationToken ct) => await Execute(MissionAction.Reassign, id, body, ct);

    /// <summary>Accepts an assigned mission for the response member's current team.</summary>
    /// <param name="id">The emergency ID.</param>
    /// <param name="body">Optional notes for the action.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>No content when the mission is accepted.</returns>
    /// <response code="204">The mission and emergency were accepted.</response>
    /// <response code="400">The emergency ID or notes are invalid.</response>
    /// <response code="401">Authentication failed.</response>
    /// <response code="403">The caller has no current team association or is not assigned to this mission.</response>
    /// <response code="404">The emergency or active mission was not found.</response>
    /// <response code="409">The mission is not in an acceptable state or a concurrency conflict occurred.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost("emergencies/{id:int}/accept"), Authorize(Roles = "ResponseTeamMember")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Accept(int id, NotesRequest? body, CancellationToken ct) => await Execute(MissionAction.Accept, id, body, ct);

    /// <summary>Rejects an assigned mission for the response member's current team.</summary>
    /// <param name="id">The emergency ID.</param>
    /// <param name="body">Optional notes for the rejection.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>No content when the mission is rejected.</returns>
    /// <response code="204">The mission was aborted and the emergency was cancelled.</response>
    /// <response code="400">The emergency ID or notes are invalid.</response>
    /// <response code="401">Authentication failed.</response>
    /// <response code="403">The caller has no current team association or is not assigned to this mission.</response>
    /// <response code="404">The emergency or active mission was not found.</response>
    /// <response code="409">The mission is not assigned or a concurrency conflict occurred.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost("emergencies/{id:int}/reject"), Authorize(Roles = "ResponseTeamMember")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Reject(int id, NotesRequest? body, CancellationToken ct) => await Execute(MissionAction.Reject, id, body, ct);

    /// <summary>Starts an assigned or accepted mission and marks the emergency as on the way.</summary>
    /// <param name="id">The emergency ID.</param>
    /// <param name="body">Optional notes for the action.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>No content when the mission starts.</returns>
    /// <response code="204">The mission is en route.</response>
    /// <response code="400">The emergency ID or notes are invalid.</response>
    /// <response code="401">Authentication failed.</response>
    /// <response code="403">The caller has no current team association or is not assigned to this mission.</response>
    /// <response code="404">The emergency or active mission was not found.</response>
    /// <response code="409">The mission/emergency state or concurrency check prevented starting.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost("emergencies/{id:int}/start"), Authorize(Roles = "ResponseTeamMember")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Start(int id, NotesRequest? body, CancellationToken ct) => await Execute(MissionAction.Start, id, body, ct);

    /// <summary>Marks an en-route mission as on scene and the emergency as arrived.</summary>
    /// <param name="id">The emergency ID.</param>
    /// <param name="body">Optional notes for the action.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>No content when arrival is recorded.</returns>
    /// <response code="204">The mission arrived on scene.</response>
    /// <response code="400">The emergency ID or notes are invalid.</response>
    /// <response code="401">Authentication failed.</response>
    /// <response code="403">The caller has no current team association or is not assigned to this mission.</response>
    /// <response code="404">The emergency or active mission was not found.</response>
    /// <response code="409">The mission/emergency state or concurrency check prevented arrival.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost("emergencies/{id:int}/arrive"), Authorize(Roles = "ResponseTeamMember")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Arrive(int id, NotesRequest? body, CancellationToken ct) => await Execute(MissionAction.Arrive, id, body, ct);

    /// <summary>Resolves an emergency and completes its on-scene mission.</summary>
    /// <param name="id">The emergency ID.</param>
    /// <param name="body">Optional completion notes.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>No content when the emergency is resolved.</returns>
    /// <response code="204">The emergency was resolved and the mission completed.</response>
    /// <response code="400">The emergency ID or notes are invalid.</response>
    /// <response code="401">Authentication failed.</response>
    /// <response code="403">The caller is not an allowed role, or a response member is not assigned to the mission.</response>
    /// <response code="404">The emergency or active mission was not found.</response>
    /// <response code="409">The emergency/mission is not ready to resolve or a concurrency conflict occurred.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost("emergencies/{id:int}/resolve"), Authorize(Roles = "Dispatcher,Admin,ResponseTeamMember")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Resolve(int id, NotesRequest? body, CancellationToken ct) => await Execute(MissionAction.Resolve, id, body, ct);

    /// <summary>Closes an emergency that has already been resolved.</summary>
    /// <param name="id">The emergency ID.</param>
    /// <param name="body">Optional notes for the action.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>No content when the emergency is closed.</returns>
    /// <response code="204">The emergency was closed.</response>
    /// <response code="400">The emergency ID or notes are invalid.</response>
    /// <response code="401">Authentication failed.</response>
    /// <response code="403">The caller is not a Dispatcher or Admin.</response>
    /// <response code="404">The emergency was not found.</response>
    /// <response code="409">The emergency is not resolved or a concurrency conflict occurred.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost("emergencies/{id:int}/close"), Authorize(Roles = "Dispatcher,Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Close(int id, NotesRequest? body, CancellationToken ct) => await Execute(MissionAction.Close, id, body, ct);

    /// <summary>Cancels an eligible emergency and aborts its active mission, if any.</summary>
    /// <param name="id">The emergency ID.</param>
    /// <param name="body">Optional cancellation notes.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>No content when cancellation succeeds.</returns>
    /// <response code="204">The emergency was cancelled.</response>
    /// <response code="400">The emergency ID or notes are invalid.</response>
    /// <response code="401">Authentication failed.</response>
    /// <response code="403">The caller lacks an allowed role, or a citizen does not own the emergency.</response>
    /// <response code="404">The emergency was not found.</response>
    /// <response code="409">The emergency is not cancellable or a concurrency conflict occurred.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost("emergencies/{id:int}/cancel"), Authorize(Roles = "Dispatcher,Admin,Citizen")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Cancel(int id, NotesRequest? body, CancellationToken ct) => await Execute(MissionAction.Cancel, id, body, ct);

    /// <summary>Lists available teams matching the emergency's required specialization.</summary>
    /// <param name="id">The emergency ID.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>Matching available teams with no active mission, ordered by name.</returns>
    /// <response code="200">The eligible teams were returned.</response>
    /// <response code="401">Authentication failed.</response>
    /// <response code="403">The caller is not a Dispatcher or Admin.</response>
    /// <response code="404">The emergency was not found.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet("emergencies/{id:int}/available-teams"), Authorize(Roles = "Dispatcher,Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<TeamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AvailableTeams(int id, CancellationToken ct) => Ok(await sender.Send(new GetAvailableTeamsQuery(id), ct));

    /// <summary>Gets the emergency's workflow and escalation history, newest first.</summary>
    /// <param name="id">The emergency ID.</param>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>History actions with actor, status changes, timestamp, and notes.</returns>
    /// <response code="200">The history was returned; an emergency with no history returns an empty array.</response>
    /// <response code="401">Authentication failed.</response>
    /// <response code="403">The caller is not a Dispatcher or Admin.</response>
    /// <response code="404">The emergency was not found.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet("emergencies/{id:int}/history"), Authorize(Roles = "Dispatcher,Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<HistoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> History(int id, CancellationToken ct)
    {
        try { return Ok(await sender.Send(new GetMissionHistoryQuery(id), ct)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
    }
    /// <summary>Lists missions; response-team members see missions for their current team only.</summary>
    /// <param name="ct">Request cancellation token.</param>
    /// <returns>Missions ordered by assignment time, newest first.</returns>
    /// <response code="200">The mission list was returned.</response>
    /// <response code="401">Authentication failed.</response>
    /// <response code="403">The caller is not an allowed role or a response member has no current team association.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet("missions"), Authorize(Roles = "Dispatcher,Admin,ResponseTeamMember")]
    [ProducesResponseType(typeof(IReadOnlyList<MissionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

    /// <summary>Request body for assignment and reassignment actions.</summary>
    /// <param name="TeamId">Identifier of the response team to assign.</param>
    public sealed record TeamRequest(int TeamId);

    /// <summary>Optional notes supplied for a mission workflow action.</summary>
    /// <param name="Notes">Notes recorded in the emergency history, limited by validation to 1,000 characters.</param>
    public sealed record NotesRequest(string? Notes);
}
