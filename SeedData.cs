using Microsoft.AspNetCore.Identity;
using OnlineShoppingSite.Models;
using System.Threading.Tasks;

namespace OnlineShoppingSite
{
    public static class SeedData
    {
        public static async Task InitializeAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            string adminRole = "Admin";
            string adminEmail = "admin@example.com";
            string adminPassword = "Admin@123";

            // Create Admin role if it doesn't exist
            if (!await roleManager.RoleExistsAsync(adminRole))
            {
                var role = new IdentityRole(adminRole);
                await roleManager.CreateAsync(role);
            }

            // Create Admin user if it doesn't exist
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser { UserName = adminEmail, Email = adminEmail };
                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, adminRole);
                }
            }
        }

        public static async Task SeedCategoriesAndItems(ApplicationDbContext context)
        {
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { Name = "T-Shirts" },
                    new Category { Name = "Hoodies" },
                    new Category { Name = "Accessories" }
                };

                context.Categories.AddRange(categories);
                await context.SaveChangesAsync();
            }

            if (!context.Items.Any())
            {
                var tShirtCategory = context.Categories.FirstOrDefault(c => c.Name == "T-Shirts");
                var hoodieCategory = context.Categories.FirstOrDefault(c => c.Name == "Hoodies");
                var accessoriesCategory = context.Categories.FirstOrDefault(c => c.Name == "Accessories");

                var items = new List<Item>
                {
                    new Item { Name = "Basic T-Shirt", Price = 19.99M, CategoryId = tShirtCategory.CategoryId, Description = "A basic t-shirt.", ImageUrl = "..." },
                    new Item { Name = "Cool Hoodie", Price = 39.99M, CategoryId = hoodieCategory.CategoryId, Description = "A cool hoodie.", ImageUrl = "..." },
                    new Item { Name = "Stylish Cap", Price = 14.99M, CategoryId = accessoriesCategory.CategoryId, Description = "A stylish cap.", ImageUrl = "..." }
                };

                context.Items.AddRange(items);
                await context.SaveChangesAsync();
            }
        }

    }
}