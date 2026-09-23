using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ResQ.Application.Common.Interfaces;
using ResQ.Application.Features.Auth.Commands.Login;
using ResQ.Application.Features.Auth.Commands.Register;

namespace ResQ.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService,
        IOptions<JwtSettings> jwtOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _jwtSettings = jwtOptions.Value;
    }

    public async Task<RegisterResponseDto> RegisterAsync(RegisterCommand command, CancellationToken cancellationToken = default)
    {
        var existingUser = await _userManager.FindByEmailAsync(command.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException($"User with email '{command.Email}' already exists.");
        }

        var user = new ApplicationUser
        {
            UserName = command.Email,
            Email = command.Email,
            FullName = command.FullName
        };

        var createResult = await _userManager.CreateAsync(user, command.Password);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"User registration failed: {errors}");
        }

        var roleResult = await _userManager.AddToRoleAsync(user, command.Role);
        if (!roleResult.Succeeded)
        {
            var errors = string.Join("; ", roleResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to assign role '{command.Role}': {errors}");
        }

        return new RegisterResponseDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            command.Role);
    }

    public async Task<LoginResponseDto> LoginAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(command.Email);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var signInResult = await _signInManager.CheckPasswordSignInAsync(user, command.Password, lockoutOnFailure: false);
        if (!signInResult.Succeeded)
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var token = await _jwtTokenService.GenerateTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiryMinutes);

        return new LoginResponseDto(
            token,
            user.Id,
            user.Email ?? string.Empty,
            user.FullName,
            roles.ToList().AsReadOnly(),
            expiresAt);
    }
}
