using Bankout.API.DTOs;
using Bankout.API.Models.Enums;
using Bankout.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bankout.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BankoutController : ControllerBase
{
    private readonly IBankoutService _bankoutService;

    public BankoutController(IBankoutService bankoutService)
    {
        _bankoutService = bankoutService;
    }

    [HttpPost]
    public async Task<ActionResult<BankoutListItemResponse>> Create([FromBody] CreateBankoutRequest request)
    {
        try
        {
            var result = await _bankoutService.CreateAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<BankoutListItemResponse>>> GetList(
        [FromQuery] string? userName,
        [FromQuery] string? requestBankId,
        [FromQuery] StatusActionEnum? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var filter = new BankoutFilterRequest(userName, requestBankId, status, fromDate, toDate, page, pageSize);
        return Ok(await _bankoutService.GetPagedAsync(filter));
    }

    [HttpGet("agents")]
    public async Task<ActionResult<IReadOnlyList<AgentOptionResponse>>> GetAgents()
    {
        return Ok(await _bankoutService.GetAgentOptionsAsync());
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<ActionResult<BankoutListItemResponse>> Approve(Guid id)
    {
        try
        {
            return Ok(await _bankoutService.ApproveAsync(id));
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

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<BankoutListItemResponse>> Cancel(Guid id)
    {
        try
        {
            return Ok(await _bankoutService.CancelAsync(id));
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
