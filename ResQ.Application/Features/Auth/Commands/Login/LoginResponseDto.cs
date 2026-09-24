namespace ResQ.Application.Features.Auth.Commands.Login;

/// <summary>JWT and account information returned after successful login.</summary>
/// <param name="Token">Signed bearer token.</param>
/// <param name="UserId">Identity identifier of the user.</param>
/// <param name="Email">Account email address.</param>
/// <param name="FullName">User display name.</param>
/// <param name="Roles">Roles assigned to the user.</param>
/// <param name="ExpiresAt">UTC token expiration time.</param>
public record LoginResponseDto(
    string Token,
    string UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles,
    DateTime ExpiresAt);
