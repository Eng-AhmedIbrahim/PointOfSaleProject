namespace Pos.Repository.Identity;

public static class AppIdentityDbContextSeed
{
    private static readonly List<string> potentialFilePaths =
    [
        Path.Combine("Data", "DataSeed","JsonFiles"),
        Path.Combine("..", "Pos.Repository", "Data", "DataSeed","JsonFiles"),
        Path.Combine("f:", "PointOfSaleProject", "src", "Pos.Repository", "Data", "DataSeed","JsonFiles"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "DataSeed","JsonFiles"),
    ];

    public static async Task SeedAsync(UserManager<AppUser> userManager, RoleManager<ApplicationRole> roleManager, AppIdentityDbContext context)
    {

        if (!context.Roles.Any())
        {
            var roles = (await GetDataFromJsonFile<ApplicationRole>("roles.json")).ToList();
            foreach (var role in roles)
            {
                await roleManager.CreateAsync(role);
            }
        }

        var allDefinedPermissions = await GetDataFromJsonFile<Permission>("permissions.json");
        foreach (var perm in allDefinedPermissions)
        {
            var existing = await context.Permissions.FirstOrDefaultAsync(p => p.Name == perm.Name);
            if (existing == null)
            {
                await context.Permissions.AddAsync(perm);
            }
            else
            {
                // Update localized names if they changed
                existing.NameAr = perm.NameAr;
                existing.NameEn = perm.NameEn;
                context.Permissions.Update(existing);
            }
        }
        await context.SaveChangesAsync();

        if (!await userManager.Users.AnyAsync())
        {
            var user = new AppUser()
            {
                Email = "Administrator",
                UserName = "Administrator",
                RegistrationDate = DateTime.Now,
                EmailConfirmed = true,
                IsActive = true
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

        if (!await userManager.Users.AnyAsync(u => u.UserName == "CaptainMorning"))
        {
            var user = new AppUser()
            {
                Email = "CaptainMorning@pos.com",
                UserName = "CaptainMorning",
                RegistrationDate = DateTime.Now,
                EmailConfirmed = true,
                DisplayName = "كابتن صالة صباحي",
                ArabicName = "كابتن صالة صباحي",
                IsActive = true
            };
            var result = await userManager.CreateAsync(user, "123456");
            if (result.Succeeded)
            {
                var role = await roleManager.FindByIdAsync("5");
                if (role != null && !string.IsNullOrEmpty(role.Name))
                {
                    await userManager.AddToRoleAsync(user, role.Name);
                }
            }
        }

        if (!await userManager.Users.AnyAsync(u => u.UserName == "CaptainEvening"))
        {
            var user = new AppUser()
            {
                Email = "CaptainEvening@pos.com",
                UserName = "CaptainEvening",
                RegistrationDate = DateTime.Now,
                EmailConfirmed = true,
                DisplayName = "كابتن صالة مسائي",
                ArabicName = "كابتن صالة مسائي",
                IsActive = true
            };
            var result = await userManager.CreateAsync(user, "123456");
            if (result.Succeeded)
            {
                var role = await roleManager.FindByIdAsync("5");
                if (role != null && !string.IsNullOrEmpty(role.Name))
                {
                    await userManager.AddToRoleAsync(user, role.Name);
                }
            }
        }

        // Always Update Claims if needed
        await SeedRoleClaims(roleManager, context);

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
        var options = new JsonSerializerOptions { AllowTrailingCommas = true, PropertyNameCaseInsensitive = true };
        return JsonSerializer.Deserialize<List<T>>(data, options) ?? [];
    }


    public static async Task SeedRoleClaims(RoleManager<ApplicationRole> roleManager, AppIdentityDbContext context)
    {
        var allPermissions = await context.Permissions.Select(p => p.Name).ToListAsync();
        
        // Administrator gets ALL permissions
        var adminRole = await roleManager.FindByNameAsync("Administrator");
        if (adminRole != null)
        {
            var claims = await roleManager.GetClaimsAsync(adminRole);
            foreach (var permission in allPermissions)
            {
                if (!claims.Any(c => c.Type == "Permission" && c.Value == permission))
                {
                    await roleManager.AddClaimAsync(adminRole, new Claim("Permission", permission));
                }
            }
        }

        // Define other roles with comprehensive permissions
        var rolePermissions = new Dictionary<string, List<string>>
        {
            { "مدير فرع", new List<string> { 
                "CanAccessTables", "CanAccessDelivery", "CanAccessTakeAway", "CanAccessDistribution",
                "CanAccessOrders", "CanAccessReport", "CanAccessSummary", "CanAccessSettings",
                "CanAccessVoidOrder", "CanAccessTransferTable", "CanAccessMergeTable", "CanAccessSplitOrder",
                "CanAccessDiscount", "CanAccessPrintReceipt", "CanAccessCloseOrder", "CanAccessVoidItem",
                "CanAccessUsers", "CanAccessRoles", "CanAccessPosSettings", "CanAccessPrintingSettings",
                "CanAccessDeliveryOrderBtn", "CanAccessDeliveryAddNewBtn", "CanAccessDeliveryClearBtn", 
                "CanAccessDeliveryComplaintsBtn", "CanAccessDeliverySearchBtn", "CanAccessDeliveryBranchManagementBtn",
                "CanAccessDeliveryDistributionBtn", "CanAccessDeliveryToggleDirectionBtn"
            } },
            { "مساعد مدير", new List<string> { 
                "CanAccessTables", "CanAccessDelivery", "CanAccessTakeAway", 
                "CanAccessOrders", "CanAccessSummary", "CanAccessReport",
                "CanAccessTransferTable", "CanAccessMergeTable", "CanAccessSplitOrder",
                "CanAccessDiscount", "CanAccessPrintReceipt", "CanAccessCloseOrder", "CanAccessVoidItem",
                "CanAccessDeliveryOrderBtn", "CanAccessDeliveryAddNewBtn", "CanAccessDeliveryClearBtn", 
                "CanAccessDeliveryComplaintsBtn", "CanAccessDeliverySearchBtn", "CanAccessDeliveryToggleDirectionBtn"
            } },
            { "كاشير", new List<string> { 
                "CanAccessTakeAway", "CanAccessDelivery", "CanAccessTables",
                "CanAccessOrders", "CanAccessDiscount", "CanAccessPrintReceipt", 
                "CanAccessCloseOrder", "CanAccessWaiting",
                "CanAccessDeliveryOrderBtn", "CanAccessDeliveryAddNewBtn", "CanAccessDeliveryClearBtn", 
                "CanAccessDeliverySearchBtn", "CanAccessDeliveryToggleDirectionBtn"
            } },
            { "كابتن صاله", new List<string> { 
                "CanAccessTables", "CanAccessOrders", "CanAccessPrintReceipt",
                "CanAccessTransferTable", "CanAccessMergeTable", "CanAccessWaiting"
            } },
            { "Call Center", new List<string> { 
                "CanAccessDelivery", "CanAccessDistribution", "CanAccessOrders",
                "CanAccessPrintReceipt", "CanAccessCloseOrder",
                "CanAccessDeliveryOrderBtn", "CanAccessDeliveryAddNewBtn", "CanAccessDeliveryClearBtn", 
                "CanAccessDeliveryComplaintsBtn", "CanAccessDeliverySearchBtn", "CanAccessDeliveryBranchManagementBtn",
                "CanAccessDeliveryDistributionBtn", "CanAccessDeliveryToggleDirectionBtn"
            } }
        };

        foreach (var role in rolePermissions)
        {
            var identityRole = await roleManager.FindByNameAsync(role.Key);
            if (identityRole != null)
            {
                 var claims = await roleManager.GetClaimsAsync(identityRole);
                foreach (var permission in role.Value)
                {
                    if (allPermissions.Contains(permission) && !claims.Any(c => c.Type == "Permission" && c.Value == permission))
                    {
                        await roleManager.AddClaimAsync(identityRole, new Claim("Permission", permission));
                    }
                }
            }
        }
    }

}