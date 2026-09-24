namespace ResQ.Application.Features.Auth.Commands.Register;

/// <summary>Information returned after account registration.</summary>
/// <param name="UserId">Identity identifier of the created user.</param>
/// <param name="Email">Registered email address.</param>
/// <param name="FullName">Registered display name.</param>
/// <param name="Role">Role assigned by public registration.</param>
public record RegisterResponseDto(
    string UserId,
    string Email,
    string FullName,
    string Role);
