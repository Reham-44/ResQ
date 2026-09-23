using MediatR;
using ResQ.Application.Common.Interfaces;

namespace ResQ.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponseDto>
{
    private readonly IAuthService _authService;

    public RegisterCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<RegisterResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
        => _authService.RegisterAsync(request, cancellationToken);
}
