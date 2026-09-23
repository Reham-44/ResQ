using MediatR;

namespace ResQ.Application.Features.Auth.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string FullName,
    string Role) : IRequest<RegisterResponseDto>;
