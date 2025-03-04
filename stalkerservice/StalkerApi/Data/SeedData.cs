using Microsoft.AspNetCore.Identity;
using StalkerModels.Data;
using StalkerModels.Models;

namespace StalkerApi.Data;

public static class SeedData
{
    public static async Task Initialize(
        IServiceProvider serviceProvider,
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        using var context = new AppDbContext(
            serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());

        // Return if data exists
        if (context.Users.Any())
            return;

        // Seed Roles
        var roles = new[] { "Admin", "Reseller", "Customer" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        // Seed Admin User
        var adminUser = new User
        {
            UserName = "admin@example.com",
            Email = "admin@example.com",
            FirstName = "Admin",
            LastName = "User",
            EmailConfirmed = true,
            IsActive = true
        };

        if (await userManager.FindByEmailAsync(adminUser.Email) == null)
        {
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Seed Default Currency
        if (!context.Currencies.Any())
        {
            context.Currencies.AddRange(
                new Currency
                {
                    Code = "USD",
                    Name = "US Dollar",
                    Symbol = "$",
                    ExchangeRate = 1.0m,
                    IsActive = true,
                    IsDefault = true,
                    Format = "{symbol}{price}"
                },
                new Currency
                {
                    Code = "EUR",
                    Name = "Euro",
                    Symbol = "€",
                    ExchangeRate = 0.92m,
                    IsActive = true,
                    IsDefault = false,
                    Format = "{symbol}{price}"
                },
                new Currency
                {
                    Code = "GBP",
                    Name = "British Pound",
                    Symbol = "£",
                    ExchangeRate = 0.79m,
                    IsActive = true,
                    IsDefault = false,
                    Format = "{symbol}{price}"
                }
            );
        }

        // Seed Categories
        if (!context.Categories.Any())
        {
            var electronics = new Category
            {
                Name = "Electronics",
                Description = "Electronic devices and accessories",
                Slug = "electronics",
                IsActive = true,
                DisplayOrder = 1
            };

            var clothing = new Category
            {
                Name = "Clothing",
                Description = "Fashion and apparel",
                Slug = "clothing",
                IsActive = true,
                DisplayOrder = 2
            };

            context.Categories.AddRange(electronics, clothing);
            await context.SaveChangesAsync();

            // Add subcategories
            context.Categories.AddRange(
                new Category
                {
                    Name = "Smartphones",
                    Description = "Mobile phones and accessories",
                    Slug = "smartphones",
                    ParentCategory = electronics,
                    IsActive = true,
                    DisplayOrder = 1
                },
                new Category
                {
                    Name = "Laptops",
                    Description = "Notebooks and accessories",
                    Slug = "laptops",
                    ParentCategory = electronics,
                    IsActive = true,
                    DisplayOrder = 2
                },
                new Category
                {
                    Name = "Men's Wear",
                    Description = "Clothing for men",
                    Slug = "mens-wear",
                    ParentCategory = clothing,
                    IsActive = true,
                    DisplayOrder = 1
                },
                new Category
                {
                    Name = "Women's Wear",
                    Description = "Clothing for women",
                    Slug = "womens-wear",
                    ParentCategory = clothing,
                    IsActive = true,
                    DisplayOrder = 2
                }
            );
        }

        // Save changes
        await context.SaveChangesAsync();
    }
}
