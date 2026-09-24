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

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EmergenciesController : ControllerBase
{
    private readonly ISender _sender;

    public EmergenciesController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [Authorize(Roles = "Citizen")]
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

    [HttpGet("{id:int}")]
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

    [HttpGet("my")]
    [Authorize(Roles = "Citizen")]
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

    [HttpGet("active")]
    [Authorize(Roles = "Dispatcher,Admin")]
    public async Task<ActionResult<IReadOnlyList<EmergencySummaryDto>>> GetActive()
    {
        var result = await _sender.Send(new GetActiveEmergenciesQuery());
        return Ok(result);
    }
}
