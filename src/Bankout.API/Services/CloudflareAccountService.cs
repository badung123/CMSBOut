using Bankout.API.Data;
using Bankout.API.DTOs;
using Bankout.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bankout.API.Services;

public interface ICloudflareAccountService
{
    Task<IReadOnlyList<CloudflareAccountResponse>> GetAllAsync();
    Task<CloudflareAccountResponse> CreateAsync(SaveCloudflareAccountRequest request, string actor);
    Task<CloudflareAccountResponse> UpdateAsync(Guid id, SaveCloudflareAccountRequest request, string actor);
    Task DeleteAsync(Guid id);
}

public class CloudflareAccountService : ICloudflareAccountService
{
    private readonly ApplicationDbContext _context;

    public CloudflareAccountService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CloudflareAccountResponse>> GetAllAsync()
    {
        return await _context.CloudflareAccounts
            .OrderByDescending(a => a.CreatedDate)
            .Select(a => new CloudflareAccountResponse(
                a.Id,
                a.UserCloudflare,
                a.ZoneId,
                a.AccountId,
                a.ApiToken,
                a.ListDomainRedirect,
                a.Status,
                a.CreatedDate,
                a.CreatedBy,
                a.UpdatedDate,
                a.UpdatedBy))
            .ToListAsync();
    }

    public async Task<CloudflareAccountResponse> CreateAsync(SaveCloudflareAccountRequest request, string actor)
    {
        var values = Normalize(request);
        await EnsureZoneIdAvailableAsync(values.ZoneId, null);

        var now = DateTime.UtcNow;
        var entity = new CloudflareAccount
        {
            Id = Guid.NewGuid(),
            UserCloudflare = values.UserCloudflare,
            ZoneId = values.ZoneId,
            AccountId = values.AccountId,
            ApiToken = values.ApiToken,
            ListDomainRedirect = values.ListDomainRedirect,
            Status = request.Status,
            CreatedDate = now,
            CreatedBy = actor
        };

        _context.CloudflareAccounts.Add(entity);
        await _context.SaveChangesAsync();
        return ToResponse(entity);
    }

    public async Task<CloudflareAccountResponse> UpdateAsync(Guid id, SaveCloudflareAccountRequest request, string actor)
    {
        var entity = await _context.CloudflareAccounts.FindAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản Cloudflare.");

        var values = Normalize(request);
        await EnsureZoneIdAvailableAsync(values.ZoneId, id);

        entity.UserCloudflare = values.UserCloudflare;
        entity.ZoneId = values.ZoneId;
        entity.AccountId = values.AccountId;
        entity.ApiToken = values.ApiToken;
        entity.ListDomainRedirect = values.ListDomainRedirect;
        entity.Status = request.Status;
        entity.UpdatedDate = DateTime.UtcNow;
        entity.UpdatedBy = actor;

        await _context.SaveChangesAsync();
        return ToResponse(entity);
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await _context.CloudflareAccounts.FindAsync(id)
            ?? throw new KeyNotFoundException("Không tìm thấy tài khoản Cloudflare.");

        _context.CloudflareAccounts.Remove(entity);
        await _context.SaveChangesAsync();
    }

    private async Task EnsureZoneIdAvailableAsync(string zoneId, Guid? excludeId)
    {
        var exists = await _context.CloudflareAccounts.AnyAsync(a =>
            a.ZoneId == zoneId && (excludeId == null || a.Id != excludeId));

        if (exists)
            throw new ArgumentException("Zone ID đã tồn tại.");
    }

    private static NormalizedAccount Normalize(SaveCloudflareAccountRequest request)
    {
        var userCloudflare = request.UserCloudflare?.Trim() ?? string.Empty;
        var zoneId = request.ZoneId?.Trim() ?? string.Empty;
        var accountId = request.AccountId?.Trim() ?? string.Empty;
        var apiToken = request.ApiToken?.Trim() ?? string.Empty;
        var domains = request.ListDomainRedirect?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(userCloudflare))
            throw new ArgumentException("User Cloudflare là bắt buộc.");
        if (string.IsNullOrWhiteSpace(zoneId))
            throw new ArgumentException("Zone ID là bắt buộc.");
        if (string.IsNullOrWhiteSpace(accountId))
            throw new ArgumentException("Account ID là bắt buộc.");
        if (apiToken.Length > 512)
            throw new ArgumentException("API Token quá dài.");
        if (string.IsNullOrWhiteSpace(domains))
            throw new ArgumentException("Danh sách domain redirect là bắt buộc.");

        return new NormalizedAccount(userCloudflare, zoneId, accountId, apiToken, domains);
    }

    private static CloudflareAccountResponse ToResponse(CloudflareAccount account) =>
        new(
            account.Id,
            account.UserCloudflare,
            account.ZoneId,
            account.AccountId,
            account.ApiToken,
            account.ListDomainRedirect,
            account.Status,
            account.CreatedDate,
            account.CreatedBy,
            account.UpdatedDate,
            account.UpdatedBy);

    private sealed record NormalizedAccount(
        string UserCloudflare,
        string ZoneId,
        string AccountId,
        string ApiToken,
        string ListDomainRedirect);
}
