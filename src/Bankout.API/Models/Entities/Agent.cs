namespace Bankout.API.Models.Entities;

public class Agent
{
    public int Id { get; set; }
    public string AgentName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }

    public ICollection<BankoutRequest> BankoutRequests { get; set; } = new List<BankoutRequest>();
}
