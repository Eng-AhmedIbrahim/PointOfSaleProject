namespace Pos.Repository.Data.DataSeed;

public static class PosDbContextDataSeed
{
    private static readonly List<string> potentialFilePaths =
    [
        Path.Combine("Data", "DataSeed","JsonFiles"),
        Path.Combine("..", "Pos.Repository", "Data", "DataSeed","JsonFiles"),
        Path.Combine("f:", "PointOfSaleProject", "src", "Pos.Repository", "Data", "DataSeed","JsonFiles"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "DataSeed","JsonFiles"),
    ];

    public async static Task SeedAsync(AppDbContext _dbContext)
    {
        if (!_dbContext.Companies.Any())
        {
            var companies = await GetDataFromJsonFile<Company>("company.json");
            if (companies != null && companies.Any())
            {
                companies[0].CreationDate = DateTime.Now;
                await _dbContext.Companies.AddAsync(companies[0]);
                await _dbContext.SaveChangesAsync();
            }
        }

        Company? company = await _dbContext.Companies.FirstOrDefaultAsync();

        if (!_dbContext.Branches.Any())
        {
            var branches = await GetDataFromJsonFile<Branch>("branch.json");
            if (branches != null && branches.Any())
            {
                branches[0].CreationDate = DateTime.Now;
                if (company != null)
                {
                    branches[0].CompanyId = company.Id;
                }
                await _dbContext.Branches.AddAsync(branches[0]);
                await _dbContext.SaveChangesAsync();
            }
        }

        Branch? branch = await _dbContext.Branches.FirstOrDefaultAsync();

        if(!_dbContext.AppDate.Any() && branch != null)
        {
            var appDate = new AppDate()
            {
                BranchId = branch.Id,
                PosDate = DateTime.Now.Date,
                StoreDate = DateTime.Now.Date,
               CurrentOrderNumber = 1
            };
            await _dbContext.AppDate.AddAsync(appDate);
            await _dbContext.SaveChangesAsync();
        }

        if (!_dbContext.KitchenTypes.Any())
        {
            var kitchenTypes = await GetDataFromJsonFile<KitchenType>("kitchenTypes.json");
            if (branch != null)
            {
                foreach (var kitchenType in kitchenTypes)
                    kitchenType.BranchId = branch!.Id;
            }
            await _dbContext.KitchenTypes.AddRangeAsync(kitchenTypes);
            await _dbContext.SaveChangesAsync();
        }

        if (!_dbContext.OrderSettings.Any())
        {
            var orderSettings = await GetDataFromJsonFile<OrderSetting>("orderSettings.json");
            if (branch != null)
            {
                foreach (var orderSetting in orderSettings)
                    orderSetting.BranchID = branch!.Id;
            }
            await _dbContext.OrderSettings.AddRangeAsync(orderSettings);
            await _dbContext.SaveChangesAsync();
        }


        if (!_dbContext.TableGroups.Any())
        {
            var tableGroups = await GetDataFromJsonFile<TableGroup>("TableGroups.json");
            if (branch != null)
            {
                foreach (var tableGroup in tableGroups)
                {
                    tableGroup.BranchID = branch!.Id;
                    tableGroup.CreationDate = DateTime.Now;
                }
            }
            await _dbContext.TableGroups.AddRangeAsync(tableGroups);
            await _dbContext.SaveChangesAsync();
        }

        if (!_dbContext.Tables.Any() && _dbContext.TableGroups.Any())
        {
            var tables = await GetDataFromJsonFile<Table>("Tables.json");
            if (branch != null)
            {
                foreach (var table in tables)
                    table.BranchID = branch!.Id;
            }
            await _dbContext.Tables.AddRangeAsync(tables);
            await _dbContext.SaveChangesAsync();
        }

        if(!_dbContext.DeliveryCustomerTitle.Any())
        {
            var deliveryCustomerTitles = await GetDataFromJsonFile<DeliveryCustomerTitle>("DeliveryCustomerTitle.json");
            
            await _dbContext.DeliveryCustomerTitle.AddRangeAsync(deliveryCustomerTitles);
            await _dbContext.SaveChangesAsync();
        }



        //if (!_dbContext.PrintingSettings.Any())
        //{
        //    var printingSettings = await GetDataFromJsonFile<PrintingSettings>("printingSettings.json");
        //    if (branch != null)
        //    {
        //        foreach (var printingSetting in printingSettings)
        //            printingSetting.BranchID = branch!.Id;
        //    }
        //    await _dbContext.PrintingSettings.AddRangeAsync(printingSettings);
        //    await _dbContext.SaveChangesAsync();
        //}
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
}