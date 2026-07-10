namespace Bankout.API.DTOs;

public record LoginRequest(string UserName, string Password);

public record LoginResponse(
    string Token,
    DateTime ExpiresAt,
    string UserName,
    string FullName,
    IReadOnlyList<string> Roles);

public record UserInfoResponse(
    string UserName,
    string FullName,
    string Email,
    IReadOnlyList<string> Roles);
