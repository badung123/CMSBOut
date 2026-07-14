using System.Net.Http.Json;
using System.Text.Json;
using Bankout.API.DTOs;
using Bankout.API.Helpers;
using Microsoft.Extensions.Options;

namespace Bankout.API.Services;

public interface IPartnerBankService
{
    Task<IReadOnlyList<PartnerBankItem>> GetBankListAsync();
    Task<PartnerPayOutResponse> RequestPayOutAsync(
        string requestId,
        string bankNo,
        string accountNumber,
        string accountName,
        double amount);
    string ComputePayOutSignature(string requestId);
    string ComputeCallbackSignature(string requestId, string transId);
}

public class PartnerBankService : IPartnerBankService
{
    private readonly HttpClient _httpClient;
    private readonly PartnerBankSettings _settings;

    public PartnerBankService(HttpClient httpClient, IOptions<PartnerBankSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
    }

    public async Task<IReadOnlyList<PartnerBankItem>> GetBankListAsync()
    {
        var url = $"{_settings.BaseUrl.TrimEnd('/')}/api/B_REQUEST_BANK_LIST?api_key={Uri.EscapeDataString(_settings.ApiKey)}";
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var banks = await response.Content.ReadFromJsonAsync<List<PartnerBankApiItem>>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<PartnerBankApiItem>();

        return banks
            .Where(b => !string.IsNullOrWhiteSpace(b.BankNo))
            .Select(b => new PartnerBankItem(b.BankNo!, b.BankName ?? b.BankNo!, b.ShortBankName ?? b.BankNo!))
            .ToList();
    }

    public async Task<PartnerPayOutResponse> RequestPayOutAsync(
        string requestId,
        string bankNo,
        string accountNumber,
        string accountName,
        double amount)
    {
        var signature = ComputePayOutSignature(requestId);
        var payload = new
        {
            api_key = _settings.ApiKey,
            request_id = requestId,
            bankno = bankNo,
            account_number = accountNumber,
            account_name = accountName,
            amount = (long)amount,
            signature
        };

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/api/B_REQUEST_PAY_OUT";
        var response = await _httpClient.PostAsJsonAsync(url, payload);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<PartnerPayOutApiResponse>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return new PartnerPayOutResponse(result?.Status ?? 0, result?.Message ?? "Unknown response");
    }

    public string ComputePayOutSignature(string requestId)
        => Md5Helper.Hash($"{_settings.ApiKey}{requestId}{_settings.Pin}");

    public string ComputeCallbackSignature(string requestId, string transId)
        => Md5Helper.Hash($"{requestId}{transId}{_settings.Pin}");

    private sealed class PartnerBankApiItem
    {
        public string? BankNo { get; set; }
        public string? BankName { get; set; }
        public string? ShortBankName { get; set; }
    }

    private sealed class PartnerPayOutApiResponse
    {
        public int Status { get; set; }
        public string? Message { get; set; }
    }
}
