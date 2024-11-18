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
    }
}