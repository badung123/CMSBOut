using Bankout.API.Data;
using Bankout.API.DTOs;
using Bankout.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bankout.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = SeedData.AdminRole)]
public class AgentsController : ControllerBase
{
    private readonly IAgentService _agentService;

    public AgentsController(IAgentService agentService)
    {
        _agentService = agentService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AgentResponse>>> GetAll()
    {
        return Ok(await _agentService.GetAllAsync());
    }

    [HttpPost]
    public async Task<ActionResult<AgentResponse>> Create([FromBody] CreateAgentRequest request)
    {
        try
        {
            var result = await _agentService.CreateAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<AgentResponse>> Update(int id, [FromBody] UpdateAgentRequest request)
    {
        try
        {
            return Ok(await _agentService.UpdateAsync(id, request));
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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _agentService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
