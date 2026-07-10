using Bankout.API.Data;
using Bankout.API.DTOs;
using Bankout.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bankout.API.Services;

public interface IAgentService
{
    Task<IReadOnlyList<AgentResponse>> GetAllAsync();
    Task<AgentResponse> CreateAsync(CreateAgentRequest request);
    Task<AgentResponse> UpdateAsync(int id, UpdateAgentRequest request);
    Task DeleteAsync(int id);
}

public class AgentService : IAgentService
{
    private readonly ApplicationDbContext _context;

    public AgentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AgentResponse>> GetAllAsync()
    {
        return await _context.Agents
            .OrderByDescending(a => a.CreatedDate)
            .Select(a => new AgentResponse(a.Id, a.AgentName, a.CreatedDate))
            .ToListAsync();
    }

    public async Task<AgentResponse> CreateAsync(CreateAgentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AgentName))
            throw new ArgumentException("Agent name is required.");

        var entity = new Agent
        {
            AgentName = request.AgentName.Trim(),
            CreatedDate = DateTime.UtcNow
        };

        _context.Agents.Add(entity);
        await _context.SaveChangesAsync();

        return new AgentResponse(entity.Id, entity.AgentName, entity.CreatedDate);
    }

    public async Task<AgentResponse> UpdateAsync(int id, UpdateAgentRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AgentName))
            throw new ArgumentException("Agent name is required.");

        var entity = await _context.Agents.FindAsync(id)
            ?? throw new KeyNotFoundException("Agent not found.");

        entity.AgentName = request.AgentName.Trim();
        await _context.SaveChangesAsync();

        return new AgentResponse(entity.Id, entity.AgentName, entity.CreatedDate);
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Agents.FindAsync(id)
            ?? throw new KeyNotFoundException("Agent not found.");

        var hasRequests = await _context.BankoutRequests.AnyAsync(b => b.AgentId == id);
        if (hasRequests)
            throw new InvalidOperationException("Cannot delete agent that has bankout requests.");

        _context.Agents.Remove(entity);
        await _context.SaveChangesAsync();
    }
}
