namespace ResQ.Application.Features.Auth.Commands.Login;

public record LoginResponseDto(
    string Token,
    string UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    DateTime ExpiresAt);
