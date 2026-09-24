using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResQ.Application.Common.Models;
using ResQ.Application.Features.Emergencies.Commands.CreateEmergency;
using ResQ.Application.Features.Emergencies.DTOs;
using ResQ.Application.Features.Emergencies.Queries.GetActiveEmergencies;
using ResQ.Application.Features.Emergencies.Queries.GetEmergencyById;
using ResQ.Application.Features.Emergencies.Queries.GetMyEmergencies;

namespace ResQ.API.Controllers;

/// <summary>Creates and queries emergencies.</summary>
/// <remarks>All actions require a valid JWT. Resource visibility and role requirements are documented per action.</remarks>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmergenciesController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes the controller with its MediatR sender.</summary>
    /// <param name="sender">Sender used to dispatch emergency requests.</param>
    public EmergenciesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Creates an emergency for the authenticated citizen.</summary>
    /// <param name="command">Emergency type, description, coordinates, and priority. Citizen ID is taken from the JWT.</param>
    /// <returns>The new emergency ID and tracking number, with a location for GetById.</returns>
    /// <response code="201">The emergency was created.</response>
    /// <response code="400">The request is invalid, the emergency type does not exist, or validation failed.</response>
    /// <response code="401">The token is missing/invalid or has no user identifier.</response>
    /// <response code="403">The caller does not have the Citizen role.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost]
    [Authorize(Roles = "Citizen")]
    [ProducesResponseType(typeof(CreateEmergencyResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CreateEmergencyResponseDto>> Create([FromBody] CreateEmergencyCommand command)
    {
        var citizenId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(citizenId))
            return Unauthorized();

        command.CitizenId = citizenId;

        var result = await _sender.Send(command);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Gets emergency details visible to the authenticated caller.</summary>
    /// <param name="id">The emergency ID.</param>
    /// <returns>Emergency details when the caller is staff or owns the emergency.</returns>
    /// <response code="200">The emergency was found and is visible to the caller.</response>
    /// <response code="401">Authentication failed or the token has no user identifier.</response>
    /// <response code="404">The emergency does not exist or is not visible to this caller.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(EmergencyDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EmergencyDetailDto>> GetById(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var isStaff = User.IsInRole("Dispatcher") || User.IsInRole("Admin");

        var result = await _sender.Send(new GetEmergencyByIdQuery(id, userId, isStaff));
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>Gets the authenticated citizen's emergencies, newest first.</summary>
    /// <param name="pageNumber">One-based page number; values less than or equal to zero use page 1.</param>
    /// <param name="pageSize">Requested page size; values less than or equal to zero use 10 and values above 100 are capped at 100.</param>
    /// <returns>A page of emergency summaries and pagination metadata.</returns>
    /// <response code="200">The page was returned.</response>
    /// <response code="400">A query parameter could not be bound to an integer.</response>
    /// <response code="401">Authentication failed or the token has no user identifier.</response>
    /// <response code="403">The caller does not have the Citizen role.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet("my")]
    [Authorize(Roles = "Citizen")]
    [ProducesResponseType(typeof(PagedResult<EmergencySummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<PagedResult<EmergencySummaryDto>>> GetMy(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var citizenId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue("sub");

        if (string.IsNullOrEmpty(citizenId))
            return Unauthorized();

        var result = await _sender.Send(new GetMyEmergenciesQuery(citizenId, pageNumber, pageSize));
        return Ok(result);
    }

    /// <summary>Lists emergencies that are not closed or cancelled for dispatcher triage.</summary>
    /// <returns>Emergency summaries ordered by descending priority and then creation time.</returns>
    /// <response code="200">The emergency list was returned. Resolved emergencies are included.</response>
    /// <response code="401">Authentication failed.</response>
    /// <response code="403">The caller is not a Dispatcher or Admin.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpGet("active")]
    [Authorize(Roles = "Dispatcher,Admin")]
    [ProducesResponseType(typeof(IReadOnlyList<EmergencySummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<EmergencySummaryDto>>> GetActive()
    {
        var result = await _sender.Send(new GetActiveEmergenciesQuery());
        return Ok(result);
    }
}
