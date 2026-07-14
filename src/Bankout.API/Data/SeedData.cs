using Bankout.API.Models.Entities;
using Bankout.API.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Bankout.API.Data;

public static class SeedData
{
    public const string AdminRole = "ADMIN";
    public const string StaffRole = "STAFF";

    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await context.Database.MigrateAsync();

        foreach (var role in new[] { AdminRole, StaffRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (!context.Balances.Any())
        {
            context.Balances.Add(new Balance { Amount = 0 });
            await context.SaveChangesAsync();
        }

        if (!await context.Agents.AnyAsync(a => a.AgentName == "LAYMA"))
        {
            context.Agents.Add(new Agent { AgentName = "LAYMA", CreatedDate = DateTime.UtcNow });
            await context.SaveChangesAsync();
        }

        if (!context.Agents.Any())
        {
            context.Agents.AddRange(
                new Agent { AgentName = "Agent A", CreatedDate = DateTime.UtcNow },
                new Agent { AgentName = "Agent B", CreatedDate = DateTime.UtcNow }
            );
            await context.SaveChangesAsync();
        }

        await SeedUserAsync(userManager, "admin", "Admin@123", AdminRole, "System Admin");
        await SeedUserAsync(userManager, "staff", "Staff@123", StaffRole, "Staff User");
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string username,
        string password,
        string role,
        string fullName)
    {
        var existing = await userManager.FindByNameAsync(username);
        if (existing != null)
            return;

        var user = new ApplicationUser
        {
            UserName = username,
            Email = $"{username}@bankout.local",
            FullName = fullName,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, password);
        if (result.Succeeded)
            await userManager.AddToRoleAsync(user, role);
    }
}
