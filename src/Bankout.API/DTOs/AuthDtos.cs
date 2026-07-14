namespace Bankout.API.DTOs;

public record LoginRequest(string UserName, string Password);

public class LoginResponse
{
    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool RequiresTwoFactor { get; set; }
    public string? PendingToken { get; set; }
}

public record VerifyTwoFactorRequest(string PendingToken, string Code);

public class UserInfoResponse
{
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public IReadOnlyList<string> Roles { get; set; } = [];
    public bool TwoFactorEnabled { get; set; }
}

public class TwoFactorStatusResponse
{
    public bool Enabled { get; set; }
}

public record TwoFactorSetupResponse(string SharedKey, string AuthenticatorUri);

public record EnableTwoFactorRequest(string Code);

public record EnableTwoFactorResponse(IReadOnlyList<string> RecoveryCodes);

public record DisableTwoFactorRequest(string Password);
