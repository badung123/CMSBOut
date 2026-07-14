using Bankout.API.DTOs;
using Bankout.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bankout.API.Controllers;

[ApiController]
[Route("api/callback")]
[AllowAnonymous]
public class CallbackController : ControllerBase
{
    private readonly IBankoutService _bankoutService;

    public CallbackController(IBankoutService bankoutService)
    {
        _bankoutService = bankoutService;
    }

    [HttpPost("payout")]
    public async Task<ActionResult<CallbackResponse>> PayoutCallback([FromBody] PayoutCallbackRequest request)
    {
        var result = await _bankoutService.ProcessCallbackAsync(request);
        return Ok(result);
    }
}
