namespace POS.Desktop.Services;

public class DesktopPrintOrderService : IPrintOrderService
{
    private readonly CommonProperties _commonProperties;
    private readonly IOrderSettingsService _orderSettingsService;
    private readonly IPrinterServices _printerServices;
    private readonly IBranchService _branchService;
    private readonly IConfiguration _configuration;

    private string LogoPath
    {
        get
        {
            try
            {
                var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images", "ReceiptLogo");
                if (!Directory.Exists(folderPath))
                    folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "images", "ReceiptLogo");
                
                if (Directory.Exists(folderPath))
                {
                    var file = Directory.GetFiles(folderPath, "*.*")
                        .FirstOrDefault(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                                            f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                                            f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase));
                    return file ?? "";
                }
            }
            catch { }
            return "";
        }
    }

    public DesktopPrintOrderService(
        CommonProperties commonProperties,
        IOrderSettingsService orderSettingsService,
        IPrinterServices printerServices,
        IBranchService branchService,
        IConfiguration configuration)
    {
        _commonProperties = commonProperties;
        _orderSettingsService = orderSettingsService;
        _printerServices = printerServices;
        _branchService = branchService;
        _configuration = configuration;
    }

    public async Task PrintDineInClosingReceipt(DineInOrderDetails order)
    {
        await PrintDineInLocally(order, isClosing: true);
    }

    public async Task PrintInitialDineInOrder(DineInOrderDetails order, 
        bool printCustomer = true, 
        bool printKitchen = true, 
        bool isClosing = false,
        bool isUpdate = false)
    {
        try
        {
            if (printCustomer)
                await PrintDineInLocally(order, isClosing, isUpdate);

            var orderSettings = _commonProperties.OrderSettings?.FirstOrDefault(o => o.OrderType == "DineIn");
            if (orderSettings != null && printKitchen)
            {
                var orderDto = new OrderDto
                {
                    OrderId = order.BasicOrderDetails!.OrderId,
                    CashierName = order.BasicOrderDetails.CashierName,
                    OrderType = "DineIn",
                    OrderDetails = order.BasicOrderDetails.Items,
                    TableName = order.RelatedTableName,
                    OrderNotice = _commonProperties.OrderNote,
                    OrderSettings = _commonProperties.OrderSettings,
                    DiscountReason = _commonProperties.OrderDiscount?.DiscountReason,
                    DiscountType = _commonProperties.OrderDiscount?.DiscountType,
                    DiscountPercentage = _commonProperties.OrderDiscount?.Percentage ?? 0,
                    TotalOrderDiscount = _commonProperties.TotalDiscount
                };

                Log.Information("PrintInitialDineInOrder: Processing {Count} items for kitchen printing", order.BasicOrderDetails.Items?.Count ?? 0);
                if (order.BasicOrderDetails.Items != null)
                {
                    foreach (var item in order.BasicOrderDetails.Items)
                    {
                        Log.Information("  Item: {Name}, ItemKitchenId: {ItemId}, CatKitchenId: {CatId}, PrintBackupFromItem: {BackupItem}, PrintBackupFromCat: {BackupCat}",
                            item.Name, item.ItemKitchenTypeId, item.CategoryKitchenTypeId, 
                            item.PrintInBackupReceiptFromItem, item.PrintInBackupReceiptFromCategory);
                    }
                }

                if (orderSettings.FullKitchenReceiptCount > 0)
                    await PrintBackupReceiptsLocally(orderDto, orderDto, isReprint: isClosing);

                if (orderSettings.SeparateReceiptCount > 0)
                    await PrintKitchenReceiptsLocally(orderDto, orderDto, isReprint: isClosing);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to print dine-in order locally");
        }
    }

    private async Task PrintDineInLocally(DineInOrderDetails order, 
        bool isClosing = false, bool isUpdate = false)
    {
        var branch = await _branchService.GetBranches();
        var currentBranch = branch.FirstOrDefault(b => b.Id == _commonProperties.BranchDetails?.Id);

        var receipt = new DineInReceipt()
        {
            Id = order.BasicOrderDetails!.OrderId,
            StoreName = currentBranch?.Name ?? "Store",
            CashierName = order.BasicOrderDetails.CashierName ?? "Cashier",
            CaptainName = order.CaptainName ?? "Captain",
            ReceiptType = "DineIn",
            DateCreated = order.BasicOrderDetails?.OrderDataTime ?? DateTime.Now,
            PaymentMethod = "Cash",
            FooterMessage = _commonProperties.DineInSettings?.OrderStatment ?? "",
            LogoPath = LogoPath, 
            LogoWidth = currentBranch?.LogoWidth ?? 150,
            TotalAmount = order.BasicOrderDetails!.Account,
            ServiceAmount = order.BasicOrderDetails.Service?.ToString("N2") ?? "0.00",
            TaxAmount = order.BasicOrderDetails.Tax?.ToString("N2") ?? "0.00",
            Discount = (order.BasicOrderDetails.Account ?? 0) + 
                       (order.BasicOrderDetails.Service ?? 0) + 
                       (order.BasicOrderDetails.Tax ?? 0) - 
                       (order.BasicOrderDetails.Total ?? 0),
            TotalOrder = order.BasicOrderDetails.Total?.ToString("N2") ?? "0.00",
        };

        var items = order.BasicOrderDetails.Items ?? new List<TableItem>();
        var orderSettings = _commonProperties.OrderSettings?.FirstOrDefault(o => o.OrderType == "DineIn");
        
        int countToPrint = 0;
        if (orderSettings != null)
        {
             countToPrint = isClosing 
                 ? (orderSettings.ClosingReceiptCount ?? 0) 
                 : (orderSettings.CustomerReceiptCount ?? 0);
        }

        Log.Information("PrintDineInLocally started. isClosing: {isClosing}, countToPrint: {countToPrint}", isClosing, countToPrint);

        int currentPrintCount = 0;
        // Only increment print count if this is NOT an update (adding items)
        if (!isUpdate)
        {
            try
            {
                currentPrintCount = await _orderSettingsService.IncrementPrintCountAsync(order.BasicOrderDetails!.OrderId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error incrementing print count for DineIn order {OrderId}", order.BasicOrderDetails!.OrderId);
            }
        }

        if (countToPrint > 0)
        {
            var kitchens = await _printerServices.GetKitchenTypesAsync();
            Log.Information("Fetched {Count} kitchens", kitchens?.Count ?? 0);
            
            var customerKitchen = kitchens?.ElementAtOrDefault(0);
            if (customerKitchen == null) Log.Warning("customerKitchen (Element 0) is null");
            
            if (customerKitchen != null)
            {
                var printers = customerKitchen.KitchenPrinters?.FirstOrDefault();
                if (printers == null) Log.Warning("printers (KitchenPrinters) is null for kitchen {KitchenName}", customerKitchen.KitchenName);

                if (printers != null)
                {

                    string? originalPath = null;
                    string? copyPath = null;
                    var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                    Directory.CreateDirectory(reportsFolder);

                    for (int i = 1; i <= countToPrint; i++)
                    {
                        var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
                        Log.Information("Iteration {i}: printerName resolved to '{printerName}'", i, printerName);
                        
                        if (string.IsNullOrEmpty(printerName)) continue;

                        // First piece of paper is original only if this is the very first print ever (count was 0, now 1)
                        // Actually, if currentPrintCount is 1, it means we just incremented it from 0 to 1.
                        // So i=1 is Original, i > 1 are Copies.
                        // If currentPrintCount was already > 0, then even i=1 is a Copy.
                        
                        bool isActuallyCopy = (currentPrintCount > 1) || (i > 1);

                        string pathToPrint;
                        if (isActuallyCopy)
                        {
                            if (copyPath == null)
                            {
                                receipt.IsCopy = true;
                                var document = new DineInReceiptDocument(receipt, items);
                                copyPath = Path.Combine(reportsFolder, $"Order_{order.BasicOrderDetails!.OrderId}_DineIn_Copy_{DateTime.Now:yyyyMMddHHmmss}_{i}.pdf");
                                Log.Information("Generating COPY PDF: {Path}", copyPath);
                                document.GeneratePdf(copyPath);
                                if (!File.Exists(copyPath)) Log.Error("Failed to generate COPY PDF at {Path}", copyPath);
                            }
                            pathToPrint = copyPath;
                        }
                        else
                        {
                            if (originalPath == null)
                            {
                                receipt.IsCopy = false;
                                var document = new DineInReceiptDocument(receipt, items);
                                originalPath = Path.Combine(reportsFolder, $"Order_{order.BasicOrderDetails!.OrderId}_DineIn_Original_{DateTime.Now:yyyyMMddHHmmss}_{i}.pdf");
                                Log.Information("Generating ORIGINAL PDF: {Path}", originalPath);
                                document.GeneratePdf(originalPath);
                                if (!File.Exists(originalPath)) Log.Error("Failed to generate ORIGINAL PDF at {Path}", originalPath);
                            }
                            pathToPrint = originalPath;
                        }

                        try
                        {
                            Log.Information("Sending PDF to printer '{PrinterName}': {Path}", printerName, pathToPrint);
                            var printSuccess = await _printerServices.PrintPdfAsync(pathToPrint, printerName);
                            Log.Information("PrintPdfAsync result: {Success} for printer {PrinterName}", printSuccess, printerName);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error printing to printer {PrinterName}", printerName);
                        }
                    }
                }
            }
        }
    }

    public async Task<bool> PrintTakeAwayOrder(decimal paid = 0,
        string customerName = "", 
        string customerPhone = "", 
        PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        BackupMainOrderDtoDetails(customerName, customerPhone, paid, paymentMethod);

        _commonProperties.OrderDto!.SkipPrintingOnServer = true;
        var result = await _orderSettingsService.CreateOrderAsync(_commonProperties.OrderDto!);

        if (result is null)
            return false;

        _commonProperties.CurrentOrderId = result.OrderId + 1;
        if (result.OrderDate.HasValue)
            _commonProperties.PosDate = DateOnly.FromDateTime(result.OrderDate.Value);
        
        try
        {
            await PrintTakeAwayLocally(_commonProperties.OrderDto!, result);

            var orderSettings = _commonProperties.OrderDto.OrderSettings?.FirstOrDefault(o => o.OrderType == "TakeAway");
            if (orderSettings != null)
            {
                if (orderSettings.FullKitchenReceiptCount > 0)
                    await PrintBackupReceiptsLocally(_commonProperties.OrderDto!, result, isReprint: true);

                if (orderSettings.SeparateReceiptCount > 0)
                    await PrintKitchenReceiptsLocally(_commonProperties.OrderDto!, result, isReprint: true);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to print takeaway order locally");
        }

        return true;
    }

    public async Task<bool> ReprintOrderAsync(int orderId)
    {
        var order = await _orderSettingsService.GetOrderByIdAsync(orderId);
        if (order == null) return false;

        if (order.OrderSettings == null || !order.OrderSettings.Any())
        {
             order.OrderSettings = _commonProperties.OrderSettings;
        }

        if (order.OrderType == "DineIn")
        {
            var dineInOrderDetails = new DineInOrderDetails
            {
                BasicOrderDetails = new BlazorBase.Models.OrderDetails
                {
                    OrderId = order.OrderId,
                    CashierName = order.CashierName,
                    Items = order.OrderDetails ?? new List<TableItem>(),
                    OrderDataTime = order.OrderDate ?? DateTime.Now,
                    Total = order.GrandTotal ?? 0,
                    Service = order.Services ?? 0,
                    Tax = order.Tax ?? 0,
                    Account = order.SubTotal ?? 0,
                    OrderDiscount = new OrderDiscount
                    {
                        Value = order.TotalDiscount ?? 0, 
                        Percentage = order.DiscountPercentage ?? 0
                    }
                },
                CaptainName = order.WaiterName ?? "Unknown", 
                RelatedTableId = order.TableId,
                RelatedTableName = order.TableName
            };
            
            await PrintInitialDineInOrder(dineInOrderDetails, true, false);
        }
        else if (order.OrderType == "Delivery")
        {
            await PrintDeliveryLocally(order, order);
        }
        else 
        {
            await PrintTakeAwayLocally(order, order);
        }

        var typeToSearch = order.OrderType == "Delivery" ? "Delivery" : "TakeAway";
        var orderSettings = order.OrderSettings?.FirstOrDefault(o => o.OrderType == typeToSearch);
        if (orderSettings != null)
        {
            if (orderSettings.FullKitchenReceiptCount > 0)
                await PrintBackupReceiptsLocally(order, order, isReprint: true);

            if (orderSettings.SeparateReceiptCount > 0)
                await PrintKitchenReceiptsLocally(order, order, isReprint: true);
        }
        
        return true;
    }

    private void BackupMainOrderDtoDetails(string customerName, 
        string customerPhone, 
        decimal paid = 0, 
        PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        _commonProperties.OrderDto!.OrderId = _commonProperties.CurrentOrderId;
        _commonProperties.OrderDto!.OrderType = "TakeAway";
        _commonProperties.OrderDto.CashierName = _commonProperties.CurrentUser;
        _commonProperties.OrderDto.BranchId = _commonProperties.BranchDetails!.Id;
        _commonProperties.OrderDto.BranchName = _commonProperties.BranchDetails.Name;
        _commonProperties.OrderDto.CashierId = _commonProperties.CurrentUserId;
        _commonProperties.OrderDto.FooterMessage = _commonProperties.TakeAwaySettings!.OrderStatment;
        _commonProperties.OrderDto.PaymentMethod = paymentMethod;
        _commonProperties.OrderDto.Paid = paid > 0.00m ? paid : (_commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 4 ? _commonProperties._financeSettingsList[4].Value : 0);
        _commonProperties.OrderDto.SubTotal = _commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 0 ? _commonProperties._financeSettingsList[0].Value : 0;
        _commonProperties.OrderDto.Services = _commonProperties.TakeAwaySettings!.Service;
        _commonProperties.OrderDto.Tax = _commonProperties.TakeAwaySettings!.Tax;
        _commonProperties.OrderDto.TotalOrderDiscount = _commonProperties.TotalDiscount;
        _commonProperties.OrderDto.DiscountPercentage = _commonProperties.OrderDiscount?.Percentage ?? 0;
        _commonProperties.OrderDto.DiscountReason = _commonProperties.OrderDiscount?.DiscountReason;
        _commonProperties.OrderDto.DiscountType = _commonProperties.OrderDiscount?.DiscountType;
        _commonProperties.OrderDto.GrandTotal = _commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 4 ? _commonProperties._financeSettingsList[4].Value : 0;
        _commonProperties.OrderDto.OrderDate = DateTime.Now;
        _commonProperties.OrderDto.OrderState = "Completed";
        _commonProperties.OrderDto.OrderNotice = _commonProperties.OrderNote;
        _commonProperties.OrderDto.OrderDetails = _commonProperties.TableItems;
        _commonProperties.OrderDto.TakeAwayCustomerName = customerName;
        _commonProperties.OrderDto.TakeawayCustomerPhone = customerPhone;
        _commonProperties.OrderDto.CustomerName = customerName;
        _commonProperties.OrderDto.CustomerPhone = customerPhone;
        _commonProperties.OrderDto.OrderSettings = _commonProperties.OrderSettings;
        _commonProperties.OrderDto.MachineName = Environment.MachineName;
    }

    private async Task PrintTakeAwayLocally(OrderDto orderDto, OrderDto createdOrder)
    {
        var branch = await _branchService.GetBranches();
        var currentBranch = branch.FirstOrDefault(b => b.Id == orderDto.BranchId);
        
        var receipt = new Receipt()
        {
            Id = createdOrder.OrderId,
            StoreName = orderDto.BranchName ?? "Store",
            CashierName = orderDto.CashierName ?? "Cashier",
            ReceiptType = orderDto.OrderType ?? "TakeAway",
            DateCreated = createdOrder.OrderDate ?? DateTime.Now,
            PaymentMethod = orderDto.PaymentMethod.ToString() ?? "Cash",
            FooterMessage = orderDto.FooterMessage ?? "",
            LogoPath = LogoPath,
            LogoWidth = currentBranch?.LogoWidth ?? 150,
            TotalAmount = orderDto.GrandTotal,
            Discount = orderDto.TotalOrderDiscount,
            Tax = orderDto.Tax,
            Services = orderDto.Services,
            SubTotal = orderDto.SubTotal,
            Items = orderDto.OrderDetails ?? new List<TableItem>()
        };

        var orderSettings = orderDto.OrderSettings?.FirstOrDefault(o => o.OrderType == "TakeAway");
        Log.Information("PrintTakeAwayLocally started. orderSettings found: {Found}, count: {Count}", orderSettings != null, orderSettings?.CustomerReceiptCount ?? 0);

        int currentPrintCount = 0;
        try
        {
            currentPrintCount = await _orderSettingsService.IncrementPrintCountAsync(createdOrder.OrderId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error incrementing print count for TakeAway order {OrderId}", createdOrder.OrderId);
        }

        if (orderSettings != null && orderSettings.CustomerReceiptCount > 0)
        {
            var kitchens = await _printerServices.GetKitchenTypesAsync();
            Log.Information("Fetched {Count} kitchens for TakeAway", kitchens?.Count ?? 0);
            
            var customerKitchen = kitchens?.ElementAtOrDefault(0);
            if (customerKitchen == null) Log.Warning("customerKitchen (Element 0) is null for TakeAway");

            if (customerKitchen != null)
            {
                var printers = customerKitchen.KitchenPrinters?.FirstOrDefault();
                if (printers == null) Log.Warning("printers is null for TakeAway kitchen {KitchenName}", customerKitchen.KitchenName);

                if (printers != null)
                {

                    string? originalPath = null;
                    string? copyPath = null;
                    var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                    Directory.CreateDirectory(reportsFolder);

                    for (int i = 1; i <= orderSettings.CustomerReceiptCount; i++)
                    {
                        var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
                        if (string.IsNullOrEmpty(printerName)) continue;

                        bool isActuallyCopy = (currentPrintCount > 1) || (i > 1);

                        string pathToPrint;
                        if (isActuallyCopy)
                        {
                            if (copyPath == null)
                            {
                                receipt.IsCopy = true;
                                var document = new ReceiptDocument(receipt, orderDto.OrderDetails ?? new List<TableItem>());
                                copyPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_TakeAway_Copy_{DateTime.Now:yyyyMMddHHmmss}_{i}.pdf");
                                document.GeneratePdf(copyPath);
                            }
                            pathToPrint = copyPath;
                        }
                        else
                        {
                            if (originalPath == null)
                            {
                                receipt.IsCopy = false;
                                var document = new ReceiptDocument(receipt, orderDto.OrderDetails ?? new List<TableItem>());
                                originalPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_TakeAway_Original_{DateTime.Now:yyyyMMddHHmmss}_{i}.pdf");
                                document.GeneratePdf(originalPath);
                            }
                            pathToPrint = originalPath;
                        }

                        try
                        {
                            await _printerServices.PrintPdfAsync(pathToPrint, printerName);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error printing TakeAway to printer {PrinterName}", printerName);
                        }
                    }
                }
            }
        }
    }

    private async Task PrintBackupReceiptsLocally(OrderDto order, OrderDto createdOrder, bool isReprint = false)
    {
        if (order.OrderDetails == null) return;

        var itemsToProcess = isReprint 
            ? order.OrderDetails 
            : order.OrderDetails.Where(i => i.DatabaseId == 0).ToList();

        if (itemsToProcess.Count == 0)
        {
            Log.Information("PrintBackupReceiptsLocally: No items to process.");
            return;
        }

        var filteredItems = itemsToProcess
            .Where(i =>
                (i.PrintInBackupReceiptFromCategory == true && i.PrintInBackupReceiptFromItem != false) ||
                (i.PrintInBackupReceiptFromCategory == false && i.PrintInBackupReceiptFromItem == true) ||
                (i.PrintInBackupReceiptFromCategory == true && i.PrintInBackupReceiptFromItem == null) ||
                (i.PrintInBackupReceiptFromCategory == null && i.PrintInBackupReceiptFromItem == null) || 
                (i.PrintInBackupReceiptFromCategory == null && i.PrintInBackupReceiptFromItem == true)
            )
            .ToList();

        if (filteredItems.Count == 0)
        {
            Log.Information("PrintBackupReceiptsLocally: No filtered items for backup receipt.");
            return;
        }

        var receipt = new KitchenReceipt()
        {
            Id = createdOrder.OrderId,
            CashierName = order.CashierName!,
            OrderType = order.OrderType ?? "TakeAway",
            DateCreated = createdOrder.OrderDate.HasValue ? new DateTimeOffset(createdOrder.OrderDate.Value) : DateTimeOffset.Now,
            Items = filteredItems,
            KitchenNote = order.OrderNotice!,
            KitchenType = "Backup",
            TableName = order.TableName
        };

        var document = new KitchenReceiptDocument(receipt, filteredItems);
        var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
        Directory.CreateDirectory(reportsFolder);
        var outputPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_{order.OrderType ?? "Type"}_Backup_{DateTime.Now:yyyyMMddHHmmss}.pdf");

        try
        {
            document.GeneratePdf(outputPath);

            var orderSettings = order.OrderSettings?.FirstOrDefault(o => o.OrderType == order.OrderType);
            var receiptCount = orderSettings?.FullKitchenReceiptCount ?? 0;

            if (receiptCount > 0)
            {
                var kitchens = await _printerServices.GetKitchenTypesAsync();
                // Use index 1 if available, otherwise index 0, otherwise first available
                var backupKitchen = kitchens?.ElementAtOrDefault(1) ?? null;
                
                if (backupKitchen != null)
                {
                    var printers = backupKitchen.KitchenPrinters?.FirstOrDefault();
                    if (printers != null)
                    {
                        for (int i = 1; i <= receiptCount; i++)
                        {
                            var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
                            if (!string.IsNullOrEmpty(printerName))
                            {
                                Log.Information("Printing backup kitchen ticket to {Printer}", printerName);
                                await _printerServices.PrintPdfAsync(outputPath, printerName);
                            }
                        }
                    }
                    else
                    {
                        Log.Warning("Backup kitchen {KitchenName} has no printers configured.", backupKitchen.KitchenName);
                    }
                }
                else
                {
                    Log.Warning("No valid backup kitchen found to print full receipt.");
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to print backup receipt locally");
        }
    }

    private async Task PrintKitchenReceiptsLocally(OrderDto order, OrderDto createdOrder, bool isReprint = false)
    {
        var orderSettings = order.OrderSettings?.FirstOrDefault(o => o.OrderType == order.OrderType);
        var receiptCount = orderSettings?.SeparateReceiptCount ?? 0;

        if (receiptCount <= 0 || order.OrderDetails == null)
        {
            Log.Information("PrintKitchenReceiptsLocally: Skipping. receiptCount: {receiptCount}, hasDetails: {hasDetails}", receiptCount, order.OrderDetails != null);
            return;
        }

        var itemsToProcess = isReprint 
            ? order.OrderDetails 
            : order.OrderDetails.Where(i => i.DatabaseId == 0).ToList();

        if (itemsToProcess.Count == 0)
        {
            Log.Information("PrintKitchenReceiptsLocally: No items to process (isReprint={isReprint}).", isReprint);
            return;
        }

        var kitchens = await _printerServices.GetKitchenTypesAsync();
        if (kitchens == null || !kitchens.Any())
        {
            Log.Warning("PrintKitchenReceiptsLocally: No kitchens found for device {Device}", Environment.MachineName);
            return;
        }

        // Group by Kitchen ID
        var groups = itemsToProcess
            .Where(item => item.ItemKitchenTypeId.HasValue || item.CategoryKitchenTypeId.HasValue)
            .GroupBy(item => item.ItemKitchenTypeId ?? item.CategoryKitchenTypeId!.Value)
            .ToList();

        Log.Information("PrintKitchenReceiptsLocally: Found {count} kitchen groups to process.", groups.Count);

        foreach (var group in groups)
        {
            var kitchenId = group.Key;
            var kitchenItems = group.ToList();
            
            // Find kitchen by ID in memory
            var kitchen = kitchens.FirstOrDefault(k => k.Id == kitchenId);
            if (kitchen == null)
            {
                Log.Warning("PrintKitchenReceiptsLocally: Kitchen ID {Id} not configured for this device. Skipping {Count} items.", kitchenId, kitchenItems.Count);
                continue;
            }

            var printers = kitchen.KitchenPrinters?.FirstOrDefault();
            if (printers == null)
            {
                Log.Warning("PrintKitchenReceiptsLocally: Kitchen {KitchenName} has no printer mapping.", kitchen.KitchenName);
                continue;
            }

            var receipt = new KitchenReceipt()
            {
                Id = createdOrder.OrderId,
                CashierName = order.CashierName!,
                OrderType = order.OrderType ?? "TakeAway",
                DateCreated = createdOrder.OrderDate.HasValue ? new DateTimeOffset(createdOrder.OrderDate.Value) : DateTimeOffset.Now,
                Items = kitchenItems,
                KitchenNote = order.OrderNotice!,
                KitchenType = kitchen.KitchenName ?? $"Kitchen_{kitchen.Id}",
                TableName = order.TableName
            };

            var document = new KitchenReceiptDocument(receipt, kitchenItems);
            var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
            Directory.CreateDirectory(reportsFolder);
            var outputPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_{order.OrderType}_{receipt.KitchenType}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

            try
            {
                document.GeneratePdf(outputPath);
                
                for (int i = 1; i <= receiptCount; i++)
                {
                    var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
                    if (!string.IsNullOrEmpty(printerName))
                    {
                        Log.Information("Printing separate kitchen ticket for {Kitchen} to {Printer}", kitchen.KitchenName, printerName);
                        await _printerServices.PrintPdfAsync(outputPath, printerName);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error printing separate kitchen ticket for {KitchenName}", kitchen.KitchenName);
            }
        }
    }
    public async Task<bool> PrintDeliveryOrder(decimal paid = 0)
    {
        await BackupDeliveryOrderDtoDetails(paid);

        _commonProperties.OrderDto!.SkipPrintingOnServer = true;
        
        OrderDto? result;
        bool isFollowUp = _commonProperties.UpdateDeliveryOrder;

        if (_commonProperties.UpdateDeliveryOrder)
            result = await _orderSettingsService.UpdateOrderAsync(_commonProperties.OrderDto!);
        else
            result = await _orderSettingsService.CreateOrderAsync(_commonProperties.OrderDto!);

        if (result is null)
            return false;

        _commonProperties.CurrentOrderId = result.OrderId + 1;
        if (result.OrderDate.HasValue)
            _commonProperties.PosDate = DateOnly.FromDateTime(result.OrderDate.Value);

        try
        {
            decimal? parentDeliveryFees = null;
            if (isFollowUp && _commonProperties.OrderDto.ParentOrderId.HasValue)
               parentDeliveryFees = _commonProperties.OrderDto.DeliveryFees;

            await PrintDeliveryLocally(_commonProperties.OrderDto!, result, isFollowUp, parentDeliveryFees);

            var orderSettings = _commonProperties.OrderDto.OrderSettings?.FirstOrDefault(o => o.OrderType == "Delivery");
            if (orderSettings != null)
            {
                if (orderSettings.FullKitchenReceiptCount > 0)
                    await PrintBackupReceiptsLocally(_commonProperties.OrderDto!, result, isReprint: !_commonProperties.UpdateDeliveryOrder);

                if (orderSettings.SeparateReceiptCount > 0)
                    await PrintKitchenReceiptsLocally(_commonProperties.OrderDto!, result, isReprint: !_commonProperties.UpdateDeliveryOrder);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to print delivery order locally");
        }

        return true;
    }

    private async Task BackupDeliveryOrderDtoDetails(decimal paid = 0)
    {
        if (!_commonProperties.UpdateDeliveryOrder)
            _commonProperties.OrderDto!.OrderId = _commonProperties.CurrentOrderId;
        
        _commonProperties.OrderDto!.OrderType = "Delivery";
        _commonProperties.OrderDto.CashierName = _commonProperties.CurrentUser;
        _commonProperties.OrderDto.BranchId = _commonProperties.BranchDetails!.Id;
        _commonProperties.OrderDto.BranchName = _commonProperties.BranchDetails.Name;
        _commonProperties.OrderDto.CashierId = _commonProperties.CurrentUserId;
        _commonProperties.OrderDto.FooterMessage = _commonProperties.DeliverySettings?.OrderStatment ?? "";
        _commonProperties.OrderDto.PaymentMethod = _commonProperties.SelectedPaymentMethod;
        _commonProperties.OrderDto.Paid = paid > 0.00m ? paid : (_commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 4 ? _commonProperties._financeSettingsList[4].Value : 0);
        _commonProperties.OrderDto.SubTotal = _commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 0 ? _commonProperties._financeSettingsList[0].Value : 0;
        _commonProperties.OrderDto.Services = _commonProperties.DeliverySettings?.Service ?? 0;
        _commonProperties.OrderDto.Tax = _commonProperties.DeliverySettings?.Tax ?? 0;
        _commonProperties.OrderDto.TotalOrderDiscount = _commonProperties.TotalDiscount;
        _commonProperties.OrderDto.DiscountPercentage = _commonProperties.OrderDiscount?.Percentage ?? 0;
        _commonProperties.OrderDto.DiscountReason = _commonProperties.OrderDiscount?.DiscountReason;
        _commonProperties.OrderDto.DiscountType = _commonProperties.OrderDiscount?.DiscountType;
        _commonProperties.OrderDto.GrandTotal = _commonProperties._financeSettingsList != null && _commonProperties._financeSettingsList.Count > 4 ? _commonProperties._financeSettingsList[4].Value : 0;
        _commonProperties.OrderDto.OrderDate = DateTime.Now;
        if (!_commonProperties.UpdateDeliveryOrder)
            _commonProperties.OrderDto.OrderState = "Pending";
        
        _commonProperties.OrderDto.OrderNotice = _commonProperties.OrderNote;
        _commonProperties.OrderDto.OrderDetails = _commonProperties.TableItems;

        _commonProperties.OrderDto.CustomerName = _commonProperties.CustomerDetails?.CustomerName;
        _commonProperties.OrderDto.Phone1 = _commonProperties.CustomerDetails?.FirstPhoneNumber;
        _commonProperties.OrderDto.Phone2 = _commonProperties.CustomerDetails?.SecondPhoneNumber;
        _commonProperties.OrderDto.StreetName = _commonProperties.CustomerDetails?.ClientAddress;
        _commonProperties.OrderDto.ZoneBonus = _commonProperties.CustomerDetails?.ZoneBonus ?? 0;
        _commonProperties.OrderDto.DeliveryFees = _commonProperties.CustomerDetails?.ZoneFees; 
        _commonProperties.OrderDto.ZoneName = _commonProperties.CustomerDetails?.ZoneName;
        _commonProperties.OrderDto.AddressNotice = _commonProperties.CustomerDetails?.AddressNote;
        _commonProperties.OrderDto.HomeNum = _commonProperties.CustomerDetails?.HomeNumber;
        _commonProperties.OrderDto.FloorNum = _commonProperties.CustomerDetails?.FloorNumber;
        _commonProperties.OrderDto.ApartmentNum = _commonProperties.CustomerDetails?.FlatNumber;
        
        _commonProperties.OrderDto.TakerID = _commonProperties.CurrentUserId;
        _commonProperties.OrderDto.TakerName = _commonProperties.CurrentUser;
        _commonProperties.OrderDto.CustomerID = _commonProperties.CustomerDetails?.Id;
        _commonProperties.OrderDto.TitleName = _commonProperties.CustomerDetails?.ClientTitle;
        _commonProperties.OrderDto.ZoneID = _commonProperties.CustomerDetails?.ZoneID;

        _commonProperties.OrderDto.OrderSettings = _commonProperties.OrderSettings;
        _commonProperties.OrderDto.MachineName = Environment.MachineName;

        if (_commonProperties.CustomerDetails?.BranchId != null)
        {
            var branches = await _branchService.GetBranches();
            var branch = branches.FirstOrDefault(b => b.Id == _commonProperties.CustomerDetails.BranchId);
            _commonProperties.OrderDto.DeliveryBranchUrl = branch?.ApiUrl;
        }
    }

    private async Task PrintDeliveryLocally(OrderDto orderDto, 
        OrderDto createdOrder, bool isFollowUp = false, decimal? parentDeliveryFees = null)
    {
        var branch = await _branchService.GetBranches();
        var currentBranch = branch.FirstOrDefault(b => b.Id == orderDto.BranchId);

        var receipt = new DeliveryReceipt()
        {
            Id = createdOrder.ParentOrderId ?? createdOrder.OrderId,
            ParentOrderId = createdOrder.ParentOrderId,
            IsFollowUp = isFollowUp,

            StoreName = orderDto.BranchName ?? "Store",
            CashierName = orderDto.CashierName ?? "Cashier",
            ReceiptType = orderDto.OrderType ?? "Delivery",
            DateCreated = createdOrder.OrderDate ?? DateTime.Now,
            PaymentMethod = orderDto.PaymentMethod.ToString() ?? "Cash",
            FooterMessage = orderDto.FooterMessage ?? "",
            LogoPath = LogoPath,
            LogoWidth = currentBranch?.LogoWidth ?? 150,
            CustomerName = orderDto.CustomerName,
            CustomerFirstPhone = orderDto.Phone1,
            CustomerSecondPhone = orderDto.Phone2,
            CustomerAddress = orderDto.StreetName,
            DeliveryName = orderDto.DriverName,
            
            DeliveryFees = (orderDto.DeliveryFees ?? 0) != 0 ? (orderDto.DeliveryFees ?? 0)
                         : (createdOrder.DeliveryFees ?? 0) != 0 ? (createdOrder.DeliveryFees ?? 0)
                         : (parentDeliveryFees ?? 0),

            ZoneName = orderDto.ZoneName,
            AddressNote = orderDto.AddressNotice,
            HomeNumber = orderDto.HomeNum,
            FloorNumber = orderDto.FloorNum,
            FlatNumber = orderDto.ApartmentNum,
            TotalAmount = orderDto.SubTotal,
            TotalOrder = orderDto.GrandTotal
        };

        var orderSettings = orderDto.OrderSettings?.FirstOrDefault(o => o.OrderType == "Delivery");
        int currentPrintCount = 0;
        try
        {
            currentPrintCount = await _orderSettingsService.IncrementPrintCountAsync(createdOrder.OrderId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error incrementing print count for Delivery order {OrderId}", createdOrder.OrderId);
        }

        if (orderSettings != null && orderSettings.CustomerReceiptCount > 0)
        {
            var kitchens = await _printerServices.GetKitchenTypesAsync();
            var customerKitchen = kitchens?.ElementAtOrDefault(0);

            if (customerKitchen != null)
            {
                var printers = customerKitchen.KitchenPrinters?.FirstOrDefault();
                if (printers != null)
                {

                    string? originalPath = null;
                    string? copyPath = null;
                    var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                    Directory.CreateDirectory(reportsFolder);

                    for (int i = 1; i <= orderSettings.CustomerReceiptCount; i++)
                    {
                        var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
                        if (string.IsNullOrEmpty(printerName)) continue;

                        bool isActuallyCopy = (currentPrintCount > 1) || (i > 1);

                        string pathToPrint;
                        if (isActuallyCopy)
                        {
                            if (copyPath == null)
                            {
                                receipt.IsCopy = true;
                                var document = new DeliveryReceiptDocument(receipt, orderDto.OrderDetails ?? new List<TableItem>());
                                copyPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_Delivery_Copy_{DateTime.Now:yyyyMMddHHmmss}_{i}.pdf");
                                document.GeneratePdf(copyPath);
                            }
                            pathToPrint = copyPath;
                        }
                        else
                        {
                            if (originalPath == null)
                            {
                                receipt.IsCopy = false;
                                var document = new DeliveryReceiptDocument(receipt, orderDto.OrderDetails ?? new List<TableItem>());
                                originalPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_Delivery_Original_{DateTime.Now:yyyyMMddHHmmss}_{i}.pdf");
                                document.GeneratePdf(originalPath);
                            }
                            pathToPrint = originalPath;
                        }

                        try
                        {
                            await _printerServices.PrintPdfAsync(pathToPrint, printerName);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error printing Delivery to printer {PrinterName}", printerName);
                        }
                    }
                }
            }
        }
    }

    public async Task PrintReceivedOrderAsync(OrderDto order)
    {
        try
        {
            order.OrderSettings = _commonProperties.OrderSettings;

            await PrintDeliveryLocally(order, order);

            var orderSettings = _commonProperties.OrderSettings?.FirstOrDefault(o => o.OrderType == "Delivery");

            if (orderSettings != null)
            {
                if (orderSettings.FullKitchenReceiptCount > 0)
                    await PrintBackupReceiptsLocally(order, order, isReprint: true);

                if (orderSettings.SeparateReceiptCount > 0)
                    await PrintKitchenReceiptsLocally(order, order, isReprint: true);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to print received order locally");
        }
    }

    public async Task PrintDispatchOrderAsync(OrderDto order)
    {
        try
        {
            order.OrderSettings = _commonProperties.OrderSettings;

            await PrintDispatchLocally(order, order);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to Print Dispatch Order Locally");
        }
    }

    private async Task PrintDispatchLocally(OrderDto orderDto, 
        OrderDto createdOrder,
        bool isFollowUp = false, 
        decimal? parentDeliveryFees = null)
    {
        var branch = await _branchService.GetBranches();
        var currentBranch = branch.FirstOrDefault(b => b.Id == orderDto.BranchId);

        var receipt = new DeliveryReceipt()
        {
            Id = createdOrder.ParentOrderId ?? createdOrder.OrderId,
            ParentOrderId = createdOrder.ParentOrderId,
            IsFollowUp = isFollowUp,

            StoreName = orderDto.BranchName ?? "Store",
            CashierName = orderDto.CashierName ?? "Cashier",
            ReceiptType = orderDto.OrderType ?? "Delivery",
            DateCreated = createdOrder.OrderDate ?? DateTime.Now,
            PaymentMethod = orderDto.PaymentMethod.ToString() ?? "Cash",
            FooterMessage = orderDto.FooterMessage ?? "",
            LogoPath = LogoPath,
            LogoWidth = currentBranch?.LogoWidth ?? 150,
            CustomerName = orderDto.CustomerName,
            CustomerFirstPhone = orderDto.Phone1,
            CustomerSecondPhone = orderDto.Phone2,
            CustomerAddress = orderDto.StreetName,
            DeliveryName = orderDto.DriverName,
            
            DeliveryFees = (orderDto.DeliveryFees ?? 0) != 0 ? (orderDto.DeliveryFees ?? 0)
                         : (createdOrder.DeliveryFees ?? 0) != 0 ? (createdOrder.DeliveryFees ?? 0)
                         : (parentDeliveryFees ?? 0),

            ZoneName = orderDto.ZoneName,
            AddressNote = orderDto.AddressNotice,
            HomeNumber = orderDto.HomeNum,
            FloorNumber = orderDto.FloorNum,
            FlatNumber = orderDto.ApartmentNum,
            TotalAmount = orderDto.SubTotal,
            TotalOrder = orderDto.GrandTotal
        };

        var orderSettings = orderDto.OrderSettings?.FirstOrDefault(o => o.OrderType == "Delivery");
        int currentPrintCount = 0;
        try
        {
            currentPrintCount = await _orderSettingsService.IncrementPrintCountAsync(createdOrder.OrderId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error incrementing print count for Dispatch order {OrderId}", createdOrder.OrderId);
        }

        if (orderSettings != null && orderSettings.CustomerReceiptCount > 0)
        {
            var kitchens = await _printerServices.GetKitchenTypesAsync();
            var customerKitchen = kitchens?.ElementAtOrDefault(0);

            if (customerKitchen != null)
            {
                var printers = customerKitchen.KitchenPrinters?.FirstOrDefault();
                if (printers != null)
                {

                    string? originalPath = null;
                    string? copyPath = null;
                    var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
                    Directory.CreateDirectory(reportsFolder);

                    for (int i = 1; i <= orderSettings.CustomerReceiptCount; i++)
                    {
                        var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
                        if (string.IsNullOrEmpty(printerName)) continue;

                        bool isActuallyCopy = (currentPrintCount > 1) || (i > 1);

                        string pathToPrint;
                        if (isActuallyCopy)
                        {
                            if (copyPath == null)
                            {
                                receipt.IsCopy = true;
                                var document = new DeliveryReceiptDocument(receipt, orderDto.OrderDetails ?? new List<TableItem>());
                                copyPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_Dispatch_Copy_{DateTime.Now:yyyyMMddHHmmss}_{i}.pdf");
                                document.GeneratePdf(copyPath);
                            }
                            pathToPrint = copyPath;
                        }
                        else
                        {
                            if (originalPath == null)
                            {
                                receipt.IsCopy = false;
                                var document = new DeliveryReceiptDocument(receipt, orderDto.OrderDetails ?? new List<TableItem>());
                                originalPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_Dispatch_Original_{DateTime.Now:yyyyMMddHHmmss}_{i}.pdf");
                                document.GeneratePdf(originalPath);
                            }
                            pathToPrint = originalPath;
                        }

                        try
                        {
                            await _printerServices.PrintPdfAsync(pathToPrint, printerName);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Error printing Dispatch to printer {PrinterName}", printerName);
                        }
                    }
                }
            }
        }
    }
}
