using Bankout.API.Data;
using Bankout.API.DTOs;
using Bankout.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bankout.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<ActionResult<DashboardResponse>> GetDashboard()
    {
        return Ok(await _dashboardService.GetDashboardAsync());
    }

    [HttpPost("balance/add")]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<ActionResult<double>> AddBalance([FromBody] UpdateBalanceRequest request)
    {
        try
        {
            var balance = await _dashboardService.AddBalanceAsync(request.Amount);
            return Ok(new { balance });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("balance/subtract")]
    [Authorize(Roles = SeedData.AdminRole)]
    public async Task<ActionResult<double>> SubtractBalance([FromBody] UpdateBalanceRequest request)
    {
        try
        {
            var balance = await _dashboardService.SubtractBalanceAsync(request.Amount);
            return Ok(new { balance });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
