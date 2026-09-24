using MediatR;

namespace ResQ.Application.Features.Auth.Commands.Login;

/// <summary>Credentials supplied to authenticate a user.</summary>
/// <param name="Email">Registered email address.</param>
/// <param name="Password">Account password.</param>
public record LoginCommand(
    string Email,
    string Password) : IRequest<LoginResponseDto>;
