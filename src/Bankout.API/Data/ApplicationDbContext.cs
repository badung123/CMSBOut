using Bankout.API.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Bankout.API.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Balance> Balances => Set<Balance>();
    public DbSet<BalanceHistory> BalanceHistories => Set<BalanceHistory>();
    public DbSet<BankoutRequest> BankoutRequests => Set<BankoutRequest>();
    public DbSet<Agent> Agents => Set<Agent>();
    public DbSet<CloudflareAccount> CloudflareAccounts => Set<CloudflareAccount>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Balance>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).IsRequired();
        });

        builder.Entity<BalanceHistory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.CreatedDate).IsRequired();

            entity.HasOne(e => e.RequestBankOut)
                .WithMany(r => r.BalanceHistories)
                .HasForeignKey(e => e.RequestBankOutId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Agent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AgentName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreatedDate).IsRequired();
        });

        builder.Entity<CloudflareAccount>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserCloudflare).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ZoneId).IsRequired().HasMaxLength(64);
            entity.Property(e => e.AccountId).IsRequired().HasMaxLength(64);
            entity.Property(e => e.ApiToken).IsRequired().HasMaxLength(512);
            entity.Property(e => e.ListDomainRedirect).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(256);
            entity.Property(e => e.UpdatedBy).HasMaxLength(256);
            entity.HasIndex(e => e.ZoneId).IsUnique();
        });

        builder.Entity<BankoutRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RequestBankId).HasMaxLength(100);
            entity.Property(e => e.UserName).HasMaxLength(100);
            entity.Property(e => e.BankAccountName).HasMaxLength(200);
            entity.Property(e => e.BankAccountNumber).HasMaxLength(50);
            entity.Property(e => e.Amount).IsRequired();
            entity.Property(e => e.BankNo).IsRequired().HasMaxLength(20);
            entity.Property(e => e.BankName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.ShortBankName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Log).HasMaxLength(2000);

            entity.HasOne(e => e.Agent)
                .WithMany(a => a.BankoutRequests)
                .HasForeignKey(e => e.AgentId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
