using MediatR;
using Microsoft.AspNetCore.Mvc;
using ResQ.Application.Features.Auth.Commands.Login;
using ResQ.Application.Features.Auth.Commands.Register;

namespace ResQ.API.Controllers;

/// <summary>Provides public user registration and login operations.</summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes the controller with its MediatR sender.</summary>
    /// <param name="sender">Sender used to dispatch authentication requests.</param>
    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Registers a new user with the Citizen role.</summary>
    /// <param name="command">Email, password, full name, and the requested role. The requested role must be Citizen.</param>
    /// <returns>The created account's ID, email, full name, and role.</returns>
    /// <response code="200">The account was created.</response>
    /// <response code="400">Request validation failed, the role is not Citizen, or Identity rejected registration.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RegisterResponseDto>> Register([FromBody] RegisterCommand command)
    {
        try
        {
            var result = await _sender.Send(command);
            return Ok(result);
        }
        catch (FluentValidation.ValidationException ex)
        {
            return BadRequest(new { errors = ex.Errors.Select(error => error.ErrorMessage).Distinct().ToArray() });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Authenticates a user and returns a JWT and account details.</summary>
    /// <param name="command">The user's email and password.</param>
    /// <returns>A signed token, user information, roles, and token expiration time.</returns>
    /// <response code="200">Credentials were accepted.</response>
    /// <response code="400">Request validation failed.</response>
    /// <response code="401">The credentials were invalid or sign-in was not successful.</response>
    /// <response code="500">An unexpected server error occurred.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginCommand command)
    {
        try
        {
            var result = await _sender.Send(command);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
