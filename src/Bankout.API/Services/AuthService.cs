using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Bankout.API.Data;
using Bankout.API.DTOs;
using Bankout.API.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Bankout.API.Services;

public interface IAuthService
{
    Task<LoginResponse?> LoginAsync(LoginRequest request);
    Task<LoginResponse?> VerifyTwoFactorAsync(VerifyTwoFactorRequest request);
    Task<UserInfoResponse?> GetCurrentUserAsync(ClaimsPrincipal user);
    Task<TwoFactorStatusResponse?> GetTwoFactorStatusAsync(ClaimsPrincipal user);
    Task<TwoFactorSetupResponse?> SetupTwoFactorAsync(ClaimsPrincipal user);
    Task<EnableTwoFactorResponse?> EnableTwoFactorAsync(ClaimsPrincipal user, string code);
    Task<bool> DisableTwoFactorAsync(ClaimsPrincipal user, string password);
}

public class AuthService : IAuthService
{
    private const string TwoFactorPendingPurpose = "2fa_pending";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IOptions<JwtSettings> jwtSettings)
    {
        _userManager = userManager;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);
        if (user == null)
            return null;

        if (await _userManager.IsLockedOutAsync(user))
            return null;

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
        {
            if (await _userManager.GetLockoutEnabledAsync(user))
                await _userManager.AccessFailedAsync(user);

            return null;
        }

        if (await _userManager.GetLockoutEnabledAsync(user))
            await _userManager.ResetAccessFailedCountAsync(user);

        if (await _userManager.GetTwoFactorEnabledAsync(user))
        {
            return new LoginResponse
            {
                UserName = user.UserName!,
                FullName = user.FullName,
                RequiresTwoFactor = true,
                PendingToken = GeneratePendingTwoFactorToken(user)
            };
        }

        return await BuildLoginResponseAsync(user);
    }

    public async Task<LoginResponse?> VerifyTwoFactorAsync(VerifyTwoFactorRequest request)
    {
        var principal = ValidatePendingTwoFactorToken(request.PendingToken);
        if (principal == null)
            return null;

        var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return null;

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return null;

        var code = request.Code.Trim().Replace(" ", string.Empty);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            code);

        if (!isValid)
        {
            var recoveryResult = await _userManager.RedeemTwoFactorRecoveryCodeAsync(user, code);
            if (!recoveryResult.Succeeded)
                return null;
        }

        return await BuildLoginResponseAsync(user);
    }

    public async Task<UserInfoResponse?> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var twoFactorEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
        return new UserInfoResponse
        {
            UserName = user.UserName!,
            FullName = user.FullName,
            Email = user.Email ?? string.Empty,
            Roles = roles.ToList(),
            TwoFactorEnabled = twoFactorEnabled
        };
    }

    public async Task<TwoFactorStatusResponse?> GetTwoFactorStatusAsync(ClaimsPrincipal principal)
    {
        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            return null;

        var enabled = await _userManager.GetTwoFactorEnabledAsync(user);
        return new TwoFactorStatusResponse { Enabled = enabled };
    }

    public async Task<TwoFactorSetupResponse?> SetupTwoFactorAsync(ClaimsPrincipal principal)
    {
        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            return null;

        if (await _userManager.GetTwoFactorEnabledAsync(user))
            throw new InvalidOperationException("2FA is already enabled.");

        await _userManager.ResetAuthenticatorKeyAsync(user);
        var key = await _userManager.GetAuthenticatorKeyAsync(user)
            ?? throw new InvalidOperationException("Unable to generate authenticator key.");

        var authenticatorUri = GenerateAuthenticatorUri("Bankout CMS", user.Email ?? user.UserName!, key);
        return new TwoFactorSetupResponse(FormatKey(key), authenticatorUri);
    }

    public async Task<EnableTwoFactorResponse?> EnableTwoFactorAsync(ClaimsPrincipal principal, string code)
    {
        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            return null;

        if (await _userManager.GetTwoFactorEnabledAsync(user))
            throw new InvalidOperationException("2FA is already enabled.");

        var normalizedCode = code.Trim().Replace(" ", string.Empty);
        var isValid = await _userManager.VerifyTwoFactorTokenAsync(
            user,
            _userManager.Options.Tokens.AuthenticatorTokenProvider,
            normalizedCode);

        if (!isValid)
            return null;

        await _userManager.SetTwoFactorEnabledAsync(user, true);
        var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
        return new EnableTwoFactorResponse((recoveryCodes ?? []).ToList());
    }

    public async Task<bool> DisableTwoFactorAsync(ClaimsPrincipal principal, string password)
    {
        var user = await _userManager.GetUserAsync(principal);
        if (user == null)
            return false;

        if (!await _userManager.CheckPasswordAsync(user, password))
            return false;

        await _userManager.SetTwoFactorEnabledAsync(user, false);
        await _userManager.ResetAuthenticatorKeyAsync(user);
        return true;
    }

    private async Task<LoginResponse> BuildLoginResponseAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);
        var token = GenerateToken(user, roles, expiresAt);
        return new LoginResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            UserName = user.UserName!,
            FullName = user.FullName,
            Roles = roles.ToList()
        };
    }

    private string GenerateToken(ApplicationUser user, IList<string> roles, DateTime expiresAt)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName!),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new("fullName", user.FullName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return WriteToken(claims, expiresAt);
    }

    private string GeneratePendingTwoFactorToken(ApplicationUser user)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.TwoFactorPendingExpirationMinutes);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new("purpose", TwoFactorPendingPurpose)
        };

        return WriteToken(claims, expiresAt);
    }

    private string WriteToken(IEnumerable<Claim> claims, DateTime expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal? ValidatePendingTwoFactorToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero
        };

        try
        {
            var principal = handler.ValidateToken(token, parameters, out _);
            if (principal.FindFirst("purpose")?.Value != TwoFactorPendingPurpose)
                return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }

    private static string GenerateAuthenticatorUri(string issuer, string accountName, string secretKey)
    {
        return string.Format(
            "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6",
            Uri.EscapeDataString(issuer),
            Uri.EscapeDataString(accountName),
            secretKey);
    }

    private static string FormatKey(string key)
    {
        var chunks = key.Chunk(4).Select(chunk => new string(chunk));
        return string.Join(' ', chunks);
    }
}
