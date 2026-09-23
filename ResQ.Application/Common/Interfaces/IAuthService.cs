using ResQ.Application.Features.Auth.Commands.Register;
using ResQ.Application.Features.Auth.Commands.Login;

namespace ResQ.Application.Common.Interfaces;

/// <summary>
/// Abstracts Identity operations so Application doesn't reference Infrastructure.
/// </summary>
public interface IAuthService
{
    Task<RegisterResponseDto> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken = default);
    Task<LoginResponseDto> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default);
}
