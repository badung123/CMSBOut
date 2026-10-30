namespace Bankout.API.DTOs;

public record CloudflareAccountResponse(
    Guid Id,
    string UserCloudflare,
    string ZoneId,
    string AccountId,
    string ApiToken,
    string ListDomainRedirect,
    bool Status,
    DateTime CreatedDate,
    string CreatedBy,
    DateTime? UpdatedDate,
    string? UpdatedBy);

public record SaveCloudflareAccountRequest(
    string UserCloudflare,
    string ZoneId,
    string AccountId,
    string ApiToken,
    string ListDomainRedirect,
    bool Status);

public record Redirect301Request(string? Domain, string? DestinationDomain);

public record Redirect301Result(string Domain, bool Success, string Action, string Message);

public record PurgeCacheRequest(string? Domain);

public record PurgeCacheResult(string Domain, bool Success, string Action, string Message);
