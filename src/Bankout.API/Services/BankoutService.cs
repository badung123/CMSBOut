using Bankout.API.Data;
using Bankout.API.DTOs;
using Bankout.API.Helpers;
using Bankout.API.Models.Entities;
using Bankout.API.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bankout.API.Services;

public interface IBankoutService
{
    Task<BankoutListItemResponse> CreateAsync(CreateBankoutRequest request);
    Task<PagedResponse<BankoutListItemResponse>> GetPagedAsync(BankoutFilterRequest filter);
    Task<IReadOnlyList<AgentOptionResponse>> GetAgentOptionsAsync();
    Task<BankoutListItemResponse> ApproveAsync(Guid id);
    Task<BankoutListItemResponse> CancelAsync(Guid id);
}

public class BankoutService : IBankoutService
{
    private const double MinAmount = 10000;
    private const double MaxAmount = 10000000;

    private readonly ApplicationDbContext _context;

    public BankoutService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<BankoutListItemResponse> CreateAsync(CreateBankoutRequest request)
    {
        ValidateCreateRequest(request);

        var agentExists = await _context.Agents.AnyAsync(a => a.Id == request.AgentId);
        if (!agentExists)
            throw new ArgumentException("Agent not found.");

        var entity = new BankoutRequest
        {
            Id = Guid.NewGuid(),
            RequestBankId = request.RequestBankId.Trim(),
            UserName = request.UserName.Trim(),
            BankAccountName = VietnameseTextHelper.ToUppercaseNoAccent(request.BankAccountName),
            BankAccountNumber = request.BankAccountNumber.Trim(),
            Amount = request.Amount,
            Bank = request.Bank.Trim(),
            AgentId = request.AgentId,
            CreatedDate = DateTime.UtcNow,
            Status = StatusActionEnum.WaitAccept,
            Log = "Created"
        };

        _context.BankoutRequests.Add(entity);
        await _context.SaveChangesAsync();

        return await MapToResponseAsync(entity.Id);
    }

    public async Task<PagedResponse<BankoutListItemResponse>> GetPagedAsync(BankoutFilterRequest filter)
    {
        var query = _context.BankoutRequests
            .Include(b => b.Agent)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.UserName))
            query = query.Where(b => b.UserName.Contains(filter.UserName.Trim()));

        if (!string.IsNullOrWhiteSpace(filter.RequestBankId))
            query = query.Where(b => b.RequestBankId.Contains(filter.RequestBankId.Trim()));

        if (filter.Status.HasValue)
            query = query.Where(b => b.Status == filter.Status.Value);

        if (filter.FromDate.HasValue)
            query = query.Where(b => b.CreatedDate >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
        {
            var toDate = filter.ToDate.Value.Date.AddDays(1);
            query = query.Where(b => b.CreatedDate < toDate);
        }

        var totalCount = await query.CountAsync();
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 10 : filter.PageSize;

        var items = await query
            .OrderByDescending(b => b.CreatedDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new BankoutListItemResponse(
                b.Id,
                b.UserName,
                b.BankAccountName,
                b.BankAccountNumber,
                b.Amount,
                b.Bank,
                b.Agent.AgentName,
                b.RequestBankId,
                b.CreatedDate,
                b.BankDate,
                b.Log,
                b.Status))
            .ToListAsync();

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResponse<BankoutListItemResponse>(items, totalCount, page, pageSize, totalPages);
    }

    public async Task<IReadOnlyList<AgentOptionResponse>> GetAgentOptionsAsync()
    {
        return await _context.Agents
            .OrderBy(a => a.AgentName)
            .Select(a => new AgentOptionResponse(a.Id, a.AgentName))
            .ToListAsync();
    }

    public async Task<BankoutListItemResponse> ApproveAsync(Guid id)
    {
        var entity = await _context.BankoutRequests
            .Include(b => b.Agent)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new KeyNotFoundException("Bankout request not found.");

        if (entity.Status != StatusActionEnum.WaitAccept)
            throw new InvalidOperationException("Only requests with WaitAccept status can be approved.");

        var balance = await _context.Balances.FirstAsync();
        if (balance.Amount < entity.Amount)
            throw new InvalidOperationException("Insufficient balance to approve this request.");

        balance.Amount -= entity.Amount;
        entity.Status = StatusActionEnum.WaitBank;
        entity.ModifiedDate = DateTime.UtcNow;
        entity.Log = AppendLog(entity.Log, "Approved");

        _context.BalanceHistories.Add(new BalanceHistory
        {
            Id = Guid.NewGuid(),
            Amount = entity.Amount,
            Type = ChangeBalanceTypeEnum.Bankout,
            RequestBankOutId = entity.Id,
            CreatedDate = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return await MapToResponseAsync(entity.Id);
    }

    public async Task<BankoutListItemResponse> CancelAsync(Guid id)
    {
        var entity = await _context.BankoutRequests
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new KeyNotFoundException("Bankout request not found.");

        if (entity.Status is StatusActionEnum.Success or StatusActionEnum.Error)
            throw new InvalidOperationException("Cannot cancel a completed request.");

        entity.Status = StatusActionEnum.Error;
        entity.ModifiedDate = DateTime.UtcNow;
        entity.Log = AppendLog(entity.Log, "Cancelled");

        await _context.SaveChangesAsync();
        return await MapToResponseAsync(entity.Id);
    }

    private static void ValidateCreateRequest(CreateBankoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RequestBankId))
            throw new ArgumentException("Request Bank ID is required.");

        if (string.IsNullOrWhiteSpace(request.UserName))
            throw new ArgumentException("UserName is required.");

        if (string.IsNullOrWhiteSpace(request.BankAccountName))
            throw new ArgumentException("Bank account name is required.");

        if (string.IsNullOrWhiteSpace(request.BankAccountNumber))
            throw new ArgumentException("Bank account number is required.");

        if (string.IsNullOrWhiteSpace(request.Bank))
            throw new ArgumentException("Bank is required.");

        if (request.Amount < MinAmount || request.Amount > MaxAmount)
            throw new ArgumentException($"Amount must be between {MinAmount} and {MaxAmount}.");
    }

    private async Task<BankoutListItemResponse> MapToResponseAsync(Guid id)
    {
        return await _context.BankoutRequests
            .Include(b => b.Agent)
            .Where(b => b.Id == id)
            .Select(b => new BankoutListItemResponse(
                b.Id,
                b.UserName,
                b.BankAccountName,
                b.BankAccountNumber,
                b.Amount,
                b.Bank,
                b.Agent.AgentName,
                b.RequestBankId,
                b.CreatedDate,
                b.BankDate,
                b.Log,
                b.Status))
            .FirstAsync();
    }

    private static string AppendLog(string? current, string message)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var entry = $"[{timestamp}] {message}";
        return string.IsNullOrWhiteSpace(current) ? entry : $"{current} | {entry}";
    }
}
