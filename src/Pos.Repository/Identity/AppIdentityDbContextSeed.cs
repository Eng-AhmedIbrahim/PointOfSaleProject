namespace Pos.Repository.Identity;

public static class AppIdentityDbContextSeed
{
    private static readonly List<string> potentialFilePaths =
  [
      Path.Combine("Data", "DataSeed","JsonFiles"),
        Path.Combine("..", "Pos.Repository", "Data", "DataSeed","JsonFiles"),
    ];

    public static async Task SeedAsync(UserManager<AppUser> userManager, RoleManager<IdentityRole> roleManager, AppIdentityDbContext context)
    {

        if (!context.Roles.Any())
        {
            var roles = await GetDataFromJsonFile<IdentityRole>("roles.json");
            context.Roles.AddRange(roles);
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
    private static string FindValidFilePath(List<string> paths, string fileName)
    {
        foreach (var path in paths)
        {
            var fullPath = Path.Combine(path, fileName);
            if (File.Exists(fullPath))
                return fullPath;
        }
        return string.Empty;
    }

    public static async Task<List<T>> GetDataFromJsonFile<T>(string fileName)
    {
        var filePath = FindValidFilePath(potentialFilePaths, fileName);
        if (string.IsNullOrEmpty(filePath))
            return [];

        var data = await File.ReadAllTextAsync(filePath);
        return JsonSerializer.Deserialize<List<T>>(data) ?? [];
    }
}