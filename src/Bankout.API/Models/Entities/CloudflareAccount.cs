namespace Bankout.API.Models.Entities;

public class CloudflareAccount
{
    public Guid Id { get; set; }
    public string UserCloudflare { get; set; } = string.Empty;
    public string ZoneId { get; set; } = string.Empty;
    public string AccountId { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;
    public string ListDomainRedirect { get; set; } = string.Empty;
    public bool Status { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedDate { get; set; }
    public string? UpdatedBy { get; set; }
}
