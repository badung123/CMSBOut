using Bankout.API.DTOs;
using Bankout.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bankout.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CloudflareAccountsController : ControllerBase
{
    private readonly ICloudflareAccountService _service;
    private readonly ICloudflarePageRuleService _pageRules;

    public CloudflareAccountsController(ICloudflareAccountService service, ICloudflarePageRuleService pageRules)
    {
        _service = service;
        _pageRules = pageRules;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CloudflareAccountResponse>>> GetAll()
    {
        return Ok(await _service.GetAllAsync());
    }

    [HttpPost]
    public async Task<ActionResult<CloudflareAccountResponse>> Create([FromBody] SaveCloudflareAccountRequest request)
    {
        try
        {
            return Ok(await _service.CreateAsync(request, Actor()));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CloudflareAccountResponse>> Update(Guid id, [FromBody] SaveCloudflareAccountRequest request)
    {
        try
        {
            return Ok(await _service.UpdateAsync(id, request, Actor()));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("redirect-301")]
    public async Task<ActionResult<Redirect301Result>> Redirect301([FromBody] Redirect301Request? request, CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(new { message = "Dữ liệu không hợp lệ." });

        var result = await _pageRules.ApplyAsync(request.Domain, request.DestinationDomain, cancellationToken);
        return Ok(result);
    }

    [HttpPost("purge-cache")]
    public async Task<ActionResult<PurgeCacheResult>> PurgeCache([FromBody] PurgeCacheRequest? request, CancellationToken cancellationToken)
    {
        if (request == null)
            return BadRequest(new { message = "Dữ liệu không hợp lệ." });

        var result = await _pageRules.PurgeEverythingAsync(request.Domain, cancellationToken);
        return Ok(result);
    }

    private string Actor() => User.Identity?.Name ?? "unknown";
}
