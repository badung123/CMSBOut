using Bankout.API.Models.Enums;

namespace Bankout.API.Models.Entities;

public class BankoutRequest
{
    public Guid Id { get; set; }
    public string RequestBankId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string BankAccountName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string Bank { get; set; } = string.Empty;
    public int AgentId { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public DateTime? BankDate { get; set; }
    public StatusActionEnum Status { get; set; }
    public string? Log { get; set; }

    public Agent Agent { get; set; } = null!;
    public ICollection<BalanceHistory> BalanceHistories { get; set; } = new List<BalanceHistory>();
}
