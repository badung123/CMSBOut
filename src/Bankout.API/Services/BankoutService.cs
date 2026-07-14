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
    Task<CallbackResponse> ProcessCallbackAsync(PayoutCallbackRequest request);
}

public class BankoutService : IBankoutService
{
    private const double MinAmount = 100000;
    private const double MaxAmount = 300000000;

    private readonly ApplicationDbContext _context;
    private readonly IPartnerBankService _partnerBankService;

    public BankoutService(ApplicationDbContext context, IPartnerBankService partnerBankService)
    {
        _context = context;
        _partnerBankService = partnerBankService;
    }

    public async Task<BankoutListItemResponse> CreateAsync(CreateBankoutRequest request)
    {
        ValidateCreateRequest(request);

        var agent = await _context.Agents.FindAsync(request.AgentId)
            ?? throw new ArgumentException("Agent not found.");

        if (string.Equals(agent.AgentName, "LAYMA", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.RequestBankId))
            throw new ArgumentException("Request Bank ID is required for LAYMA agent.");

        var bank = await ResolveBankAsync(request.BankNo);

        var entity = new BankoutRequest
        {
            Id = Guid.NewGuid(),
            RequestBankId = string.IsNullOrWhiteSpace(request.RequestBankId)
                ? null
                : request.RequestBankId.Trim(),
            UserName = string.Empty,
            BankAccountName = VietnameseTextHelper.ToUppercaseNoAccent(request.BankAccountName),
            BankAccountNumber = request.BankAccountNumber.Trim(),
            Amount = request.Amount,
            BankNo = bank.BankNo,
            BankName = bank.BankName,
            ShortBankName = bank.ShortBankName,
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

        if (!string.IsNullOrWhiteSpace(filter.RequestBankId))
            query = query.Where(b => b.RequestBankId != null && b.RequestBankId.Contains(filter.RequestBankId.Trim()));

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
                b.BankAccountName,
                b.BankAccountNumber,
                b.Amount,
                b.BankNo,
                b.BankName,
                b.ShortBankName,
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

        var payOutResult = await _partnerBankService.RequestPayOutAsync(
            entity.Id.ToString(),
            entity.BankNo,
            entity.BankAccountNumber,
            entity.BankAccountName,
            entity.Amount);

        entity.ModifiedDate = DateTime.UtcNow;

        if (payOutResult.Status == 1)
        {
            entity.Status = StatusActionEnum.WaitBank;
            entity.Log = AppendLog(entity.Log, "Gửi y/c bank thành công");
        }
        else
        {
            entity.Status = StatusActionEnum.ErrorRequestBank;
            entity.Log = AppendLog(entity.Log, payOutResult.Message);
        }

        await _context.SaveChangesAsync();
        return await MapToResponseAsync(entity.Id);
    }

    public async Task<BankoutListItemResponse> CancelAsync(Guid id)
    {
        var entity = await _context.BankoutRequests
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new KeyNotFoundException("Bankout request not found.");

        if (entity.Status is StatusActionEnum.Success or StatusActionEnum.ErrorBank)
            throw new InvalidOperationException("Cannot cancel a completed request.");

        entity.Status = StatusActionEnum.ErrorRequestBank;
        entity.ModifiedDate = DateTime.UtcNow;
        entity.Log = AppendLog(entity.Log, "Cancelled");

        await _context.SaveChangesAsync();
        return await MapToResponseAsync(entity.Id);
    }

    public async Task<CallbackResponse> ProcessCallbackAsync(PayoutCallbackRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RequestId))
            return new CallbackResponse("1", "requestId is required");

        if (!Guid.TryParse(request.RequestId, out var requestGuid))
            return new CallbackResponse("1", "requestId is invalid");

        var entity = await _context.BankoutRequests
            .FirstOrDefaultAsync(b => b.Id == requestGuid);

        if (entity == null)
            return new CallbackResponse("1", "request not found");

        var expectedSignature = _partnerBankService.ComputeCallbackSignature(request.RequestId, request.TransId);
        if (!string.Equals(expectedSignature, request.Signature, StringComparison.OrdinalIgnoreCase))
        {
            entity.Status = StatusActionEnum.ErrorBank;
            entity.ModifiedDate = DateTime.UtcNow;
            entity.Log = AppendLog(entity.Log, "Kiểm tra chữ ký không giống nhau");
            await _context.SaveChangesAsync();
            return new CallbackResponse("1", "signature not valid");
        }

        entity.ModifiedDate = DateTime.UtcNow;

        if (request.Status == 1)
        {
            entity.Status = StatusActionEnum.Success;
            entity.BankDate = ParseCallbackDate(request.Date) ?? DateTime.UtcNow;
            entity.Log = AppendLog(entity.Log, "bank thành công");

            var alreadyDeducted = await _context.BalanceHistories
                .AnyAsync(h => h.RequestBankOutId == entity.Id && h.Type == ChangeBalanceTypeEnum.Bankout);

            if (!alreadyDeducted)
            {
                var balance = await _context.Balances.FirstAsync();
                balance.Amount -= entity.Amount;

                _context.BalanceHistories.Add(new BalanceHistory
                {
                    Id = Guid.NewGuid(),
                    Amount = entity.Amount,
                    Type = ChangeBalanceTypeEnum.Bankout,
                    RequestBankOutId = entity.Id,
                    CreatedDate = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return new CallbackResponse("0", "Success");
        }

        entity.Status = StatusActionEnum.ErrorBank;
        entity.Log = AppendLog(entity.Log, request.Message);
        await _context.SaveChangesAsync();
        return new CallbackResponse("1", request.Message);
    }

    private async Task<PartnerBankItem> ResolveBankAsync(string bankNo)
    {
        var banks = await _partnerBankService.GetBankListAsync();
        var bank = banks.FirstOrDefault(b => b.BankNo == bankNo.Trim())
            ?? throw new ArgumentException("Ngân hàng không hợp lệ.");

        return bank;
    }

    private static void ValidateCreateRequest(CreateBankoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.BankAccountName))
            throw new ArgumentException("Bank account name is required.");

        if (string.IsNullOrWhiteSpace(request.BankAccountNumber))
            throw new ArgumentException("Bank account number is required.");

        if (string.IsNullOrWhiteSpace(request.BankNo))
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
                b.BankAccountName,
                b.BankAccountNumber,
                b.Amount,
                b.BankNo,
                b.BankName,
                b.ShortBankName,
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

    private static DateTime? ParseCallbackDate(string date)
    {
        if (string.IsNullOrWhiteSpace(date))
            return null;

        if (DateTime.TryParseExact(date, "dd/MM/yyyy HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var parsed))
            return parsed;

        return DateTime.TryParse(date, out var fallback) ? fallback : null;
    }
}
