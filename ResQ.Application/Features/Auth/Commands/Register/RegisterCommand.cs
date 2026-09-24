using MediatR;

namespace ResQ.Application.Features.Auth.Commands.Register;

/// <summary>Credentials and profile information for public Citizen registration.</summary>
/// <param name="Email">User email address.</param>
/// <param name="Password">Password for the new account.</param>
/// <param name="FullName">Display name for the user.</param>
/// <param name="Role">Must be exactly <c>Citizen</c> for public registration.</param>
public record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string Role) : IRequest<RegisterResponseDto>;
