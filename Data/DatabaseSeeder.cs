using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StoreApi.Models;

namespace StoreApi.Data;

public static class DatabaseSeeder
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
    {
        var dbContext = services.GetRequiredService<StoreDbContext>();
        await dbContext.Database.MigrateAsync();

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var roleName in new[] { "Admin", "Customer" })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                EnsureSucceeded(result, $"creating the {roleName} role");
            }
        }

        var email = configuration["Admin:Email"];
        var password = configuration["Admin:Password"];
        if (string.IsNullOrWhiteSpace(email) != string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException("Configure both Admin:Email and Admin:Password, or neither.");
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new AppUser { UserName = email, Email = email, FullName = "Store Administrator" };
            var createResult = await userManager.CreateAsync(admin, password);
            EnsureSucceeded(createResult, "creating the configured administrator");
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(admin, "Admin");
            EnsureSucceeded(roleResult, "assigning the administrator role");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed {operation}: {errors}");
        }
    }
}
