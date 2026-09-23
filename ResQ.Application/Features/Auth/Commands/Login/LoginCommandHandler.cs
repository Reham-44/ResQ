using MediatR;
using ResQ.Application.Common.Interfaces;

namespace ResQ.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponseDto>
{
    private readonly IAuthService _authService;

    public LoginCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public Task<LoginResponseDto> Handle(LoginCommand request, CancellationToken cancellationToken)
        => _authService.LoginAsync(request, cancellationToken);
}
