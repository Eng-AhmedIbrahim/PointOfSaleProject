namespace POS.Desktop.Services;

public class DesktopPrintOrderService : IPrintOrderService
{
    private readonly CommonProperties _commonProperties;
    private readonly OrderSettingsService _orderSettingsService;
    private readonly IPrinterServices _printerServices;
    private readonly BranchService _branchService;
    private readonly IConfiguration _configuration;

    public DesktopPrintOrderService(
        CommonProperties commonProperties,
        OrderSettingsService orderSettingsService,
        IPrinterServices printerServices,
        BranchService branchService,
        IConfiguration configuration)
    {
        _commonProperties = commonProperties;
        _orderSettingsService = orderSettingsService;
        _printerServices = printerServices;
        _branchService = branchService;
        _configuration = configuration;
    }

    public async Task PrintInitialDineInOrder(DineInOrderDetails order)
    {
        try
        {
            await PrintDineInLocally(order);

            var orderSettings = _commonProperties.OrderSettings?.FirstOrDefault(o => o.OrderType == "DineIn");
            if (orderSettings != null)
            {
                var orderDto = new OrderDto
                {
                    OrderId = order.BasicOrderDetails!.OrderId,
                    CashierName = order.BasicOrderDetails.CashierName,
                    OrderType = "DineIn",
                    OrderDetails = order.BasicOrderDetails.Items,
                    OrderNotice = _commonProperties.OrderNote,
                    OrderSettings = _commonProperties.OrderSettings,
                    DiscountReason = _commonProperties.OrderDiscount?.DiscountReason,
                    DiscountType = _commonProperties.OrderDiscount?.DiscountType,
                    DiscountPercentage = _commonProperties.OrderDiscount?.Percentage ?? 0,
                    TotalOrderDiscount = _commonProperties.TotalDiscount
                };

                if (orderSettings.FullKitchenReceiptCount > 0)
                    await PrintBackupReceiptsLocally(orderDto, orderDto);

                if (orderSettings.SeparateReceiptCount > 0)
                    await PrintKitchenReceiptsLocally(orderDto, orderDto);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to print dine-in order locally");
        }
    }

    private async Task PrintDineInLocally(DineInOrderDetails order)
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
            LogoPath = "", 
            LogoWidth = currentBranch?.LogoWidth ?? 100,
            TotalAmount = order.BasicOrderDetails.Total,
            ServiceAmount = order.BasicOrderDetails.Service.ToString() ?? string.Empty,
            TotalOrder = order.BasicOrderDetails.Total.ToString() ?? string.Empty,
        };

        var items = order.BasicOrderDetails.Items ?? new List<TableItem>();
        var document = new DineInReceiptDocument(receipt, items);
        
        var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "Reports");
        Directory.CreateDirectory(reportsFolder);
        var outputPath = Path.Combine(reportsFolder, $"Order_{order.BasicOrderDetails!.OrderId}_DineIn_{DateTime.Now:yyyyMMddHHmmss}.pdf");

        document.GeneratePdf(outputPath);

        var orderSettings = _commonProperties.OrderSettings?.FirstOrDefault(o => o.OrderType == "DineIn");
        if (orderSettings != null && orderSettings.CustomerReceiptCount > 0)
        {
            var kitchens = await _printerServices.GetKitchenTypesAsync();
            var customerKitchen = kitchens?.FirstOrDefault();
            
            if (customerKitchen != null)
            {
                var printers = customerKitchen.KitchenPrinters;
                if (printers != null)
                {
                    for (int i = 1; i <= orderSettings.CustomerReceiptCount; i++)
                    {
                        var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
                        if (!string.IsNullOrEmpty(printerName))
                        {
                            await _printerServices.PrintPdfAsync(outputPath, printerName);
                        }
                    }
                }
            }
        }
    }

    public async Task<bool> PrintTakeAwayOrder(decimal paid = 0, string customerName = "", string customerPhone = "", PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        BackupMainOrderDtoDetails(customerName, customerPhone, paid, paymentMethod);

        _commonProperties.OrderDto!.SkipPrintingOnServer = true;
        var result = await _orderSettingsService.CreateOrderAsync(_commonProperties.OrderDto!);

        if (result is null)
            return false;

        _commonProperties.CurrentOrderId = result.OrderId + 1;
        if (result.OrderDate.HasValue)
        {
            _commonProperties.PosDate = DateOnly.FromDateTime(result.OrderDate.Value);
        }

        try
        {
            await PrintTakeAwayLocally(_commonProperties.OrderDto!, result);

            var orderSettings = _commonProperties.OrderDto.OrderSettings?.FirstOrDefault(o => o.OrderType == "TakeAway");
            if (orderSettings != null)
            {
                if (orderSettings.FullKitchenReceiptCount > 0)
                    await PrintBackupReceiptsLocally(_commonProperties.OrderDto!, result);

                if (orderSettings.SeparateReceiptCount > 0)
                    await PrintKitchenReceiptsLocally(_commonProperties.OrderDto!, result);
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
            
            await PrintInitialDineInOrder(dineInOrderDetails);
        }
        else 
        {
            await PrintTakeAwayLocally(order, order);
            
            var orderSettings = order.OrderSettings?.FirstOrDefault(o => o.OrderType == order.OrderType);
            if (orderSettings != null)
            {
                if (orderSettings.FullKitchenReceiptCount > 0)
                    await PrintBackupReceiptsLocally(order, order);

                if (orderSettings.SeparateReceiptCount > 0)
                    await PrintKitchenReceiptsLocally(order, order);
            }
        }
        
        return true;
    }

    private void BackupMainOrderDtoDetails(string customerName, string customerPhone, decimal paid = 0, PaymentMethod paymentMethod = PaymentMethod.Cash)
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
            LogoPath = "",
            LogoWidth = currentBranch?.LogoWidth ?? 100,
            TotalAmount = orderDto.GrandTotal,
            Discount = orderDto.TotalOrderDiscount,
            Tax = orderDto.Tax,
            Services = orderDto.Services,
            SubTotal = orderDto.SubTotal,
            Items = orderDto.OrderDetails ?? new List<TableItem>()
        };

        var document = new ReceiptDocument(receipt, orderDto.OrderDetails ?? new List<TableItem>());
        var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "Reports");
        Directory.CreateDirectory(reportsFolder);
        var outputPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_TakeAway_{DateTime.Now:yyyyMMddHHmmss}.pdf");

        document.GeneratePdf(outputPath);

        var orderSettings = orderDto.OrderSettings?.FirstOrDefault(o => o.OrderType == "TakeAway");
        if (orderSettings != null && orderSettings.CustomerReceiptCount > 0)
        {
            var kitchens = await _printerServices.GetKitchenTypesAsync();
            var customerKitchen = kitchens?.FirstOrDefault();
            
            if (customerKitchen != null)
            {
                var printers = customerKitchen.KitchenPrinters;
                if (printers != null)
                {
                    for (int i = 1; i <= orderSettings.CustomerReceiptCount; i++)
                    {
                        var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
                        if (!string.IsNullOrEmpty(printerName))
                        {
                            await _printerServices.PrintPdfAsync(outputPath, printerName);
                        }
                    }
                }
            }
        }
    }

    private async Task PrintBackupReceiptsLocally(OrderDto order, OrderDto createdOrder)
    {
        var filteredItems = order.OrderDetails!
            .Where(i =>
                (i.PrintInBackupReceiptFromCategory == true && i.PrintInBackupReceiptFromItem != false) ||
                (i.PrintInBackupReceiptFromCategory == false && i.PrintInBackupReceiptFromItem == true) ||
                (i.PrintInBackupReceiptFromCategory == true && i.PrintInBackupReceiptFromItem == null)
            )
            .ToList();

        if (filteredItems.Count == 0)
            return;

        var receipt = new KitchenReceipt()
        {
            Id = createdOrder.OrderId,
            CashierName = order.CashierName!,
            OrderType = order.OrderType ?? "TakeAway",
            DateCreated = createdOrder.OrderDate.HasValue ? new DateTimeOffset(createdOrder.OrderDate.Value) : DateTimeOffset.Now,
            Items = filteredItems,
            KitchenNote = order.OrderNotice!,
            KitchenType = "Backup"
        };

        var document = new KitchenReceiptDocument(receipt, filteredItems);
        var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "Reports");
        Directory.CreateDirectory(reportsFolder);
        var outputPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_{order.OrderType ?? "Type"}_Backup_{DateTime.Now:yyyyMMddHHmmss}.pdf");

        document.GeneratePdf(outputPath);

        var orderSettings = order.OrderSettings?.FirstOrDefault(o => o.OrderType == order.OrderType);
        var receiptCount = orderSettings?.FullKitchenReceiptCount ?? 0;

        if (receiptCount > 0)
        {
            var kitchens = await _printerServices.GetKitchenTypesAsync();
            var backupKitchen = kitchens?.FirstOrDefault(k => k.KitchenName == "Backup");

            if (backupKitchen?.KitchenPrinters != null)
            {
                for (int i = 1; i <= receiptCount; i++)
                {
                    var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(backupKitchen.KitchenPrinters) as string;
                    if (!string.IsNullOrEmpty(printerName))
                    {
                        await _printerServices.PrintPdfAsync(outputPath, printerName);
                    }
                }
            }
        }
    }

    private async Task PrintKitchenReceiptsLocally(OrderDto order, OrderDto createdOrder)
    {
        var orderSettings = order.OrderSettings?.FirstOrDefault(o => o.OrderType == order.OrderType);
        var receiptCount = orderSettings?.SeparateReceiptCount ?? 0;

        if (receiptCount <= 0 || order.OrderDetails == null || order.OrderDetails.Count == 0)
            return;

        var kitchens = await _printerServices.GetKitchenTypesAsync();
        if (kitchens == null) return;

        var groupedItems = order.OrderDetails
            .Where(item => item.ItemKitchenTypeId.HasValue || item.CategoryKitchenTypeId.HasValue)
            .GroupBy(item => item.ItemKitchenTypeId ?? item.CategoryKitchenTypeId!.Value)
            .Join(kitchens,
                  g => g.Key,
                  k => k.Id,
                  (g, k) => new
                  {
                      KitchenId = k.Id,
                      KitchenName = k.KitchenName ?? $"Kitchen_{k.Id}",
                      Items = g.ToList(),
                      Printers = k.KitchenPrinters
                  })
            .ToList();

        foreach (var kitchenGroup in groupedItems)
        {
            var receipt = new KitchenReceipt()
            {
                Id = createdOrder.OrderId,
                CashierName = order.CashierName!,
                OrderType = order.OrderType ?? "TakeAway",
                DateCreated = createdOrder.OrderDate.HasValue ? new DateTimeOffset(createdOrder.OrderDate.Value) : DateTimeOffset.Now,
                Items = kitchenGroup.Items,
                KitchenNote = order.OrderNotice!,
                KitchenType = kitchenGroup.KitchenName
            };

            var document = new KitchenReceiptDocument(receipt, kitchenGroup.Items);
            var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wwwroot", "Reports");
            Directory.CreateDirectory(reportsFolder);
            var outputPath = Path.Combine(reportsFolder, $"Order_{createdOrder.OrderId}_{order.OrderType ?? "Type"}_{kitchenGroup.KitchenName}_{DateTime.Now:yyyyMMddHHmmss}.pdf");

            document.GeneratePdf(outputPath);

            if (kitchenGroup.Printers != null)
            {
                for (int i = 1; i <= receiptCount; i++)
                {
                    var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(kitchenGroup.Printers) as string;
                    if (!string.IsNullOrEmpty(printerName))
                    {
                        await _printerServices.PrintPdfAsync(outputPath, printerName);
                    }
                }
            }
        }
    }
}
