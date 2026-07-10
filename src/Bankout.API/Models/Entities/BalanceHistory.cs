using Bankout.API.Models.Enums;

namespace Bankout.API.Models.Entities;

public class BalanceHistory
{
    public Guid Id { get; set; }
    public double Amount { get; set; }
    public ChangeBalanceTypeEnum Type { get; set; }
    public Guid? RequestBankOutId { get; set; }
    public DateTime CreatedDate { get; set; }

    public BankoutRequest? RequestBankOut { get; set; }
}
