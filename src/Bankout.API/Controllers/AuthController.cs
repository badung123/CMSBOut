using Bankout.API.DTOs;
using Bankout.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bankout.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (result == null)
            return Unauthorized(new { message = "Invalid username or password." });

        return Ok(result);
    }

    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
    {
        var result = await _authService.VerifyTwoFactorAsync(request);
        if (result == null)
            return Unauthorized(new { message = "Invalid or expired verification code." });

        return Ok(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out successfully." });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserInfoResponse>> Me()
    {
        var result = await _authService.GetCurrentUserAsync(User);
        if (result == null)
            return Unauthorized();

        return Ok(result);
    }

    [HttpGet("2fa/status")]
    [Authorize]
    public async Task<ActionResult<TwoFactorStatusResponse>> GetTwoFactorStatus()
    {
        var result = await _authService.GetTwoFactorStatusAsync(User);
        if (result == null)
            return Unauthorized();

        return Ok(result);
    }

    [HttpPost("2fa/setup")]
    [Authorize]
    public async Task<ActionResult<TwoFactorSetupResponse>> SetupTwoFactor()
    {
        try
        {
            var result = await _authService.SetupTwoFactorAsync(User);
            if (result == null)
                return Unauthorized();

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("2fa/enable")]
    [Authorize]
    public async Task<ActionResult<EnableTwoFactorResponse>> EnableTwoFactor([FromBody] EnableTwoFactorRequest request)
    {
        try
        {
            var result = await _authService.EnableTwoFactorAsync(User, request.Code);
            if (result == null)
                return BadRequest(new { message = "Invalid verification code." });

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    public async Task<IActionResult> DisableTwoFactor([FromBody] DisableTwoFactorRequest request)
    {
        var success = await _authService.DisableTwoFactorAsync(User, request.Password);
        if (!success)
            return BadRequest(new { message = "Invalid password." });

        return Ok(new { message = "2FA disabled successfully." });
    }
}
