using Bankout.API.Data;
using Bankout.API.DTOs;
using Bankout.API.Models.Entities;
using Bankout.API.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Bankout.API.Services;

public interface IDashboardService
{
    Task<DashboardResponse> GetDashboardAsync();
    Task<double> AddBalanceAsync(double amount);
    Task<double> SubtractBalanceAsync(double amount);
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardResponse> GetDashboardAsync()
    {
        var balance = await _context.Balances.FirstAsync();
        var totalTransactions = await _context.BankoutRequests.CountAsync();
        return new DashboardResponse(balance.Amount, totalTransactions);
    }

    public async Task<double> AddBalanceAsync(double amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        var balance = await _context.Balances.FirstAsync();
        balance.Amount += amount;

        _context.BalanceHistories.Add(new BalanceHistory
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Type = ChangeBalanceTypeEnum.AddBalanceAdmin,
            CreatedDate = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return balance.Amount;
    }

    public async Task<double> SubtractBalanceAsync(double amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero.");

        var balance = await _context.Balances.FirstAsync();
        if (balance.Amount < amount)
            throw new InvalidOperationException("Insufficient balance.");

        balance.Amount -= amount;

        _context.BalanceHistories.Add(new BalanceHistory
        {
            Id = Guid.NewGuid(),
            Amount = amount,
            Type = ChangeBalanceTypeEnum.SubtractBalanceAdmin,
            CreatedDate = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return balance.Amount;
    }
}
