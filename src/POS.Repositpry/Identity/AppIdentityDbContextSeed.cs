using Microsoft.AspNetCore.Identity;
using POS.Core.Entities.Identity;

namespace POS.Repository.Identity;

public static class AppIdentityDbContextSeed
{
    public static async Task SeedUsersAsync(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, AppIdentityDbContext context)
    {

        IdentityRole role = new IdentityRole()
        {
            Name = "Administrator"
        };

        if (!await context.Roles.AnyAsync(r => r.Name == role.Name))
        {
            await context.Roles.AddAsync(role);
            await context.SaveChangesAsync();
        }

        if (!await userManager.Users.AnyAsync())
        {
            var user = new AppUser()
            {
                Email = "Administrator",
                UserName = "Administrator",
                RegistrationDate = DateTime.Now,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(user, "12312300Aa#@");

            if (result.Succeeded)
            {
                var roleResult = await userManager.AddToRoleAsync(user, "Administrator");

                if (!roleResult.Succeeded)
                {
                    var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    throw new Exception($"Role assignment failed: {roleErrors}");
                }
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new Exception($"User creation failed: {errors}");
            }
        }
    }
}