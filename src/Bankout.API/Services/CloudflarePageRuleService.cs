using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bankout.API.Data;
using Bankout.API.DTOs;
using Bankout.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bankout.API.Services;

public interface ICloudflarePageRuleService
{
    Task<Redirect301Result> ApplyAsync(string? domain, string? destinationDomain, CancellationToken cancellationToken);
    Task<PurgeCacheResult> PurgeEverythingAsync(string? domain, CancellationToken cancellationToken);
}

public class CloudflarePageRuleService : ICloudflarePageRuleService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly char[] DomainSeparators = [',', ';', '\n', '\r', '\t', ' '];

    private readonly HttpClient _http;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CloudflarePageRuleService> _logger;

    public CloudflarePageRuleService(
        IHttpClientFactory httpClientFactory,
        ApplicationDbContext context,
        ILogger<CloudflarePageRuleService> logger)
    {
        _http = httpClientFactory.CreateClient("Cloudflare");
        _context = context;
        _logger = logger;
    }

    public async Task<Redirect301Result> ApplyAsync(string? domainRaw, string? destinationRaw, CancellationToken cancellationToken)
    {
        var domain = NormalizeDomain(domainRaw);
        var destination = NormalizeDomain(destinationRaw);
        var shownDomain = string.IsNullOrEmpty(domain) ? (domainRaw ?? string.Empty).Trim() : domain;

        if (!IsValidDomain(domain))
            return new Redirect301Result(shownDomain, false, "failed", "Domain không hợp lệ.");
        if (!IsValidDomain(destination))
            return new Redirect301Result(domain, false, "failed", "Domain cuối không hợp lệ.");
        if (string.Equals(domain, destination, StringComparison.OrdinalIgnoreCase))
            return new Redirect301Result(domain, false, "failed", "Domain cuối trùng domain nguồn.");

        var accounts = await _context.CloudflareAccounts.AsNoTracking().ToListAsync(cancellationToken);
        var matches = accounts.Where(a => ContainsDomain(a.ListDomainRedirect, domain)).ToList();
        if (matches.Count == 0)
            return new Redirect301Result(domain, false, "failed", "Không tìm thấy tài khoản Cloudflare chứa domain này.");

        var enabled = matches.Where(a => a.Status).ToList();
        var targets = enabled.Count > 0 ? enabled : matches;
        var enableRules = enabled.Count > 0;

        var outcomes = new List<AccountOutcome>();
        foreach (var account in targets)
        {
            outcomes.Add(await ApplyToAccountAsync(account, domain, destination, enableRules, cancellationToken));
        }

        var success = outcomes.All(o => o.Success);
        var action = outcomes.Select(o => o.Action).Distinct().Count() == 1 ? outcomes[0].Action : "mixed";
        var message = string.Join("\n", outcomes.Select(o => o.Message));
        return new Redirect301Result(domain, success, action, message);
    }

    public async Task<PurgeCacheResult> PurgeEverythingAsync(string? domainRaw, CancellationToken cancellationToken)
    {
        var domain = NormalizeDomain(domainRaw);
        var shownDomain = string.IsNullOrEmpty(domain) ? (domainRaw ?? string.Empty).Trim() : domain;

        if (!IsValidDomain(domain))
            return new PurgeCacheResult(shownDomain, false, "failed", "Domain không hợp lệ.");

        var accounts = await _context.CloudflareAccounts.AsNoTracking().ToListAsync(cancellationToken);
        var matches = accounts.Where(a => ContainsDomain(a.ListDomainRedirect, domain)).ToList();
        if (matches.Count == 0)
            return new PurgeCacheResult(domain, false, "failed", "Không tìm thấy tài khoản Cloudflare chứa domain này.");

        var outcomes = new List<AccountOutcome>();
        var purgedZones = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var account in matches)
        {
            var zoneId = account.ZoneId?.Trim() ?? string.Empty;
            if (zoneId.Length > 0 && purgedZones.Contains(zoneId))
            {
                outcomes.Add(Ok("purged", $"Đã xóa toàn bộ cache của {domain} ({account.UserCloudflare})."));
                continue;
            }

            var outcome = await PurgeAccountAsync(account, domain, cancellationToken);
            if (outcome.Success && zoneId.Length > 0)
                purgedZones.Add(zoneId);
            outcomes.Add(outcome);
        }

        var success = outcomes.All(o => o.Success);
        var action = outcomes.Select(o => o.Action).Distinct().Count() == 1 ? outcomes[0].Action : "mixed";
        var message = string.Join("\n", outcomes.Select(o => o.Message));
        return new PurgeCacheResult(domain, success, action, message);
    }

    private async Task<AccountOutcome> PurgeAccountAsync(
        CloudflareAccount account,
        string domain,
        CancellationToken cancellationToken)
    {
        var user = account.UserCloudflare;
        if (string.IsNullOrWhiteSpace(account.ApiToken))
            return Fail($"Tài khoản {user} chưa có API Token.");

        var zoneId = account.ZoneId?.Trim() ?? string.Empty;
        if (zoneId.Length == 0 || zoneId.Length > 64 || !zoneId.All(char.IsAsciiLetterOrDigit))
            return Fail($"Zone ID của {user} không hợp lệ.");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, $"zones/{zoneId}/purge_cache");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", account.ApiToken.Trim());
            request.Content = JsonContent.Create(new PurgeEverythingBody(), options: JsonOptions);
            var response = await SendAsync<JsonElement>(request, cancellationToken);
            if (!response.Success)
            {
                var error = ErrorMessage(response);
                _logger.LogWarning("Cloudflare purge cache failed for {Domain} on zone {ZoneId}: {Error}", domain, zoneId, error);
                return Fail($"{user}: {error}");
            }

            return Ok("purged", $"Đã xóa toàn bộ cache của {domain} ({user}).");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Fail($"Cloudflare phản hồi quá lâu ({user}).");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Cloudflare purge cache failed for {Domain} on zone {ZoneId}", domain, zoneId);
            return Fail($"Không kết nối được Cloudflare ({user}).");
        }
    }

    private async Task<AccountOutcome> ApplyToAccountAsync(
        CloudflareAccount account,
        string domain,
        string destination,
        bool enable,
        CancellationToken cancellationToken)
    {
        var user = account.UserCloudflare;
        if (string.IsNullOrWhiteSpace(account.ApiToken))
            return Fail($"Tài khoản {user} chưa có API Token.");

        var zoneId = account.ZoneId?.Trim() ?? string.Empty;
        if (zoneId.Length == 0 || zoneId.Length > 64 || !zoneId.All(char.IsAsciiLetterOrDigit))
            return Fail($"Zone ID của {user} không hợp lệ.");

        var pattern = $"{domain}/*";
        var destinationUrl = $"https://{destination}/$1";

        try
        {
            var listed = await ListPageRulesAsync(zoneId, account.ApiToken.Trim(), cancellationToken);
            if (listed.Error != null)
                return Fail($"{user}: {listed.Error}");

            var existing = listed.Rules.FirstOrDefault(rule => MatchesDomainRule(rule, domain));
            var creating = existing == null || string.IsNullOrWhiteSpace(existing.Id);
            if (!enable && creating)
                return new AccountOutcome(false, "skipped", $"Tài khoản {user} đang tắt, không tạo page rule.");

            var wasActive = string.Equals(existing?.Status, "active", StringComparison.OrdinalIgnoreCase);
            var body = BuildRule(pattern, destinationUrl, enable ? "active" : "disabled", existing?.Priority);
            CfResponse<PageRuleRead> written;

            if (creating || existing == null || string.IsNullOrWhiteSpace(existing.Id))
            {
                written = await SendRuleAsync(HttpMethod.Post, $"zones/{zoneId}/pagerules", account.ApiToken.Trim(), body, cancellationToken);
            }
            else
            {
                var ruleId = Uri.EscapeDataString(existing.Id);
                written = await SendRuleAsync(HttpMethod.Put, $"zones/{zoneId}/pagerules/{ruleId}", account.ApiToken.Trim(), body, cancellationToken);
            }

            if (!written.Success)
            {
                var error = ErrorMessage(written);
                _logger.LogWarning("Cloudflare page rule failed for {Domain} on zone {ZoneId}: {Error}", domain, zoneId, error);
                return Fail($"{user}: {error}");
            }

            if (creating)
                return Ok("created", $"Đã tạo page rule 301: {pattern} → {destinationUrl} ({user}).");
            if (!enable)
                return Ok("disabled", $"Đã tắt page rule 301 của {domain} ({user}).");
            if (!wasActive)
                return Ok("enabled", $"Đã bật page rule 301: {pattern} → {destinationUrl} ({user}).");

            return Ok("updated", $"Đã cập nhật page rule 301: {pattern} → {destinationUrl} ({user}).");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Fail($"Cloudflare phản hồi quá lâu ({user}).");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Cloudflare request failed for {Domain} on zone {ZoneId}", domain, zoneId);
            return Fail($"Không kết nối được Cloudflare ({user}).");
        }
    }

    private async Task<(List<PageRuleRead> Rules, string? Error)> ListPageRulesAsync(string zoneId, string token, CancellationToken cancellationToken)
    {
        var all = new List<PageRuleRead>();
        for (var page = 1; page <= 10; page++)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"zones/{zoneId}/pagerules?page={page}&per_page=50");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await SendAsync<List<PageRuleRead>>(request, cancellationToken);
            if (!response.Success)
                return ([], ErrorMessage(response));

            if (response.Result != null)
                all.AddRange(response.Result);

            var totalPages = response.ResultInfo?.TotalPages ?? 1;
            if (page >= totalPages || response.Result == null || response.Result.Count == 0)
                break;
        }

        return (all, null);
    }

    private async Task<CfResponse<PageRuleRead>> SendRuleAsync(
        HttpMethod method,
        string url,
        string token,
        PageRuleWrite body,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = JsonContent.Create(body, options: JsonOptions);
        return await SendAsync<PageRuleRead>(request, cancellationToken);
    }

    private async Task<CfResponse<T>> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(body))
        {
            return new CfResponse<T>
            {
                Errors = [new CfMessage { Message = $"Cloudflare trả về HTTP {(int)response.StatusCode}." }]
            };
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<CfResponse<T>>(body, JsonOptions);
            if (parsed == null)
            {
                return new CfResponse<T>
                {
                    Errors = [new CfMessage { Message = "Không đọc được phản hồi Cloudflare." }]
                };
            }

            if (!parsed.Success && (parsed.Errors == null || parsed.Errors.Count == 0))
            {
                parsed.Errors = [new CfMessage { Message = $"Cloudflare trả về HTTP {(int)response.StatusCode}." }];
            }

            return parsed;
        }
        catch (JsonException)
        {
            return new CfResponse<T>
            {
                Errors = [new CfMessage { Message = $"Cloudflare trả về HTTP {(int)response.StatusCode}." }]
            };
        }
    }

    private static PageRuleWrite BuildRule(string pattern, string destinationUrl, string status, int? priority) =>
        new()
        {
            Targets =
            [
                new PageRuleTarget
                {
                    Target = "url",
                    Constraint = new PageRuleConstraint
                    {
                        Operator = "matches",
                        Value = pattern
                    }
                }
            ],
            Actions =
            [
                new PageRuleAction
                {
                    Id = "forwarding_url",
                    Value = new ForwardingUrlValue
                    {
                        Url = destinationUrl,
                        StatusCode = 301
                    }
                }
            ],
            Status = status,
            Priority = priority
        };

    private static bool MatchesDomainRule(PageRuleRead rule, string domain)
    {
        if (rule.Targets == null)
            return false;

        foreach (var target in rule.Targets)
        {
            if (!string.Equals(target.Target, "url", StringComparison.OrdinalIgnoreCase))
                continue;

            var value = target.Constraint?.Value?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(value))
                continue;

            if (value.StartsWith("https://", StringComparison.Ordinal))
                value = value["https://".Length..];
            else if (value.StartsWith("http://", StringComparison.Ordinal))
                value = value["http://".Length..];

            if (value == $"{domain}/*" || value == $"*.{domain}/*")
                return true;
        }

        return false;
    }

    private static bool ContainsDomain(string? list, string domain)
    {
        if (string.IsNullOrWhiteSpace(list))
            return false;

        foreach (var part in list.Split(DomainSeparators, StringSplitOptions.RemoveEmptyEntries))
        {
            if (string.Equals(NormalizeDomain(part), domain, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    internal static string NormalizeDomain(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var value = raw.Trim().Trim('\'', '"');
        if (value.Contains("://", StringComparison.Ordinal)
            && Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && !string.IsNullOrWhiteSpace(uri.IdnHost))
        {
            value = uri.IdnHost;
        }

        var slash = value.IndexOf('/');
        if (slash >= 0)
            value = value[..slash];

        var query = value.IndexOf('?');
        if (query >= 0)
            value = value[..query];

        if (value.StartsWith("*.", StringComparison.Ordinal))
            value = value[2..];

        if (value.EndsWith("/*", StringComparison.Ordinal))
            value = value[..^2];

        var colon = value.IndexOf(':');
        if (colon >= 0)
            value = value[..colon];

        return value.Trim().TrimEnd('.').ToLowerInvariant();
    }

    private static bool IsValidDomain(string domain)
    {
        if (domain.Length is < 4 or > 253 || !domain.Contains('.'))
            return false;

        var labels = domain.Split('.');
        if (labels.Length < 2)
            return false;

        foreach (var label in labels)
        {
            if (label.Length is < 1 or > 63 || label[0] == '-' || label[^1] == '-')
                return false;

            foreach (var c in label)
            {
                if (!(char.IsLetterOrDigit(c) || c == '-'))
                    return false;
            }
        }

        return true;
    }

    private static string ErrorMessage<T>(CfResponse<T> response)
    {
        var messages = response.Errors?
            .Select(e => e.Message)
            .Where(m => !string.IsNullOrWhiteSpace(m))
            .Cast<string>()
            .ToList();

        return messages is { Count: > 0 }
            ? string.Join("; ", messages)
            : "Cloudflare từ chối yêu cầu.";
    }

    private static AccountOutcome Ok(string action, string message) => new(true, action, message);

    private static AccountOutcome Fail(string message) => new(false, "failed", message);

    private sealed record AccountOutcome(bool Success, string Action, string Message);

    private sealed class CfResponse<T>
    {
        public bool Success { get; set; }
        public T? Result { get; set; }
        public List<CfMessage>? Errors { get; set; }
        public CfResultInfo? ResultInfo { get; set; }
    }

    private sealed class CfMessage
    {
        public string? Message { get; set; }
    }

    private sealed class CfResultInfo
    {
        public int Page { get; set; }
        public int TotalPages { get; set; }
    }

    private sealed class PageRuleRead
    {
        public string? Id { get; set; }
        public List<PageRuleTarget>? Targets { get; set; }
        public string? Status { get; set; }
        public int? Priority { get; set; }
    }

    private sealed class PageRuleWrite
    {
        public List<PageRuleTarget> Targets { get; set; } = [];
        public List<PageRuleAction> Actions { get; set; } = [];
        public string Status { get; set; } = "active";
        public int? Priority { get; set; }
    }

    private sealed class PageRuleTarget
    {
        public string Target { get; set; } = "url";
        public PageRuleConstraint Constraint { get; set; } = new();
    }

    private sealed class PageRuleConstraint
    {
        public string Operator { get; set; } = "matches";
        public string Value { get; set; } = string.Empty;
    }

    private sealed class PageRuleAction
    {
        public string Id { get; set; } = "forwarding_url";
        public ForwardingUrlValue Value { get; set; } = new();
    }

    private sealed class ForwardingUrlValue
    {
        public string Url { get; set; } = string.Empty;
        public int StatusCode { get; set; } = 301;
    }

    private sealed class PurgeEverythingBody
    {
        public bool PurgeEverything { get; set; } = true;
    }
}
