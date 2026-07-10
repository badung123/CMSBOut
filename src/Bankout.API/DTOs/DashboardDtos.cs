namespace Bankout.API.DTOs;

public record DashboardResponse(double Balance, int TotalTransactions);

public record UpdateBalanceRequest(double Amount);
