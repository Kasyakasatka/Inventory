using InventoryManagement.Web.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InventoryManagement.Web.Data;

public static class DataSeeder
{
    public static async Task EnsureSeedData(ApplicationDbContext context, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        await context.Database.MigrateAsync();

        var userManager = serviceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        await SeedRolesAsync(roleManager);

        await SeedCategoriesAsync(context);

        await SeedAdminUserAsync(userManager, roleManager, configuration);
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager)
    {
        var roleExists = await roleManager.RoleExistsAsync("Admin");
        if (!roleExists)
        {
            await roleManager.CreateAsync(new IdentityRole<Guid>("Admin"));
        }
    }

    private static async Task SeedCategoriesAsync(ApplicationDbContext context)
    {
        if (!await context.Categories.AnyAsync())
        {
            var categories = new Category[]
            {
                new Category { Id = Guid.NewGuid(), Name = "Equipment" },
                new Category { Id = Guid.NewGuid(), Name = "Office" },
                new Category { Id = Guid.NewGuid(), Name = "Books" },
                new Category { Id = Guid.NewGuid(), Name = "Furniture" },
                new Category { Id = Guid.NewGuid(), Name = "Other" }
            };
            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedAdminUserAsync(UserManager<User> userManager, RoleManager<IdentityRole<Guid>> roleManager, IConfiguration configuration)
    {
        if (await userManager.FindByNameAsync("admin") == null)
        {
            var adminUser = new User
            {
                UserName = "admin",
                Email = "admin@yopmail.com",
                EmailConfirmed = true,
                Inventories = new List<Inventory>(),
                InventoryAccesses = new List<InventoryAccess>(),
                SearchVector = null!
            };

            var adminPassword = configuration["AdminPassword"]; 
            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                var roleExists = await roleManager.RoleExistsAsync("Admin");
                if (roleExists)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
            }
        }
    }
}