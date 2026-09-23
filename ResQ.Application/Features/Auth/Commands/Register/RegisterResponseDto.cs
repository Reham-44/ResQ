namespace ResQ.Application.Features.Auth.Commands.Register;

public record RegisterResponseDto(
    string UserId,
    string Email,
    string FullName,
    string Role);
