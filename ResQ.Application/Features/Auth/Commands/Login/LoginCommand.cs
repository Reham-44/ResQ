using MediatR;

namespace ResQ.Application.Features.Auth.Commands.Login;

public record LoginCommand(
    string Email,
    string Password) : IRequest<LoginResponseDto>;
