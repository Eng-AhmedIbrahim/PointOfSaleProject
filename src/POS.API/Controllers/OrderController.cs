namespace POS.API.Controllers;

public class OrderController : BaseApiController
{
    private readonly IOrderService _orderService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IBranchService _branchService;
    private readonly IPrinterServices _printerServices;
    private readonly IKitchenServices _kitchenServices;
    private readonly IMapper _mapper;
    private readonly string _reportsFolder;
    public OrderController(IOrderService orderService,
        IWebHostEnvironment webHostEnvironment,
        IBranchService branchService,
        IPrinterServices printerServices,
        IKitchenServices kitchenServices,
        IMapper mapper)
    {
        _orderService = orderService;
        _webHostEnvironment = webHostEnvironment;
        _branchService = branchService;
        _printerServices = printerServices;
        _kitchenServices = kitchenServices;
        _mapper = mapper;
        _reportsFolder = Path.Combine(_webHostEnvironment.ContentRootPath, "Reports");
        Directory.CreateDirectory(_reportsFolder);
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(typeof(Orders), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [HttpPost("createOrder")]
    public async Task<IActionResult> CreateOrderAsync([FromBody] OrderDto orderDto)
    {
        if (orderDto == null || orderDto.OrderDetails == null || !orderDto.OrderDetails.Any())
            return BadRequest("Invalid order data.");

        var order = BackupMainOrderDetails(orderDto);

        if (Enum.TryParse<OrderTypes>(orderDto.OrderType ?? "TakeAway", out var orderType))
        {
            var orderSettings = orderDto.OrderSettings!.FirstOrDefault(o => o.OrderType == orderType.ToString());
            order.OrderType = orderType;

            if (orderType == OrderTypes.TakeAway)
            {
                BackupTakeawayOrder(orderDto, order);
                var createdOrder = await _orderService.CreateOrderAsync(order);

                if (createdOrder is null)
                    return BadRequest(new ApiResponse(404, "Order Not Created"));

                if (createdOrder!.Id != 0)
                {
                    List<string> branchDetails = await GetBranchDetails(orderDto);

                    await printTakeAwayReceipts(orderDto, createdOrder, branchDetails);

                    if (orderSettings!.FullKitchenReceiptCount > 0)
                    {
                        await PrintBackupReceipts( orderDto, createdOrder);
                    }


                    if (orderSettings.SeparateReceiptCount > 0)
                    {
                        await PrintKitchenReceipts(orderDto, createdOrder);
                    }

                    return Ok(orderDto);
                }
                else
                {
                    return BadRequest(new ApiResponse(404, "Order Not Created"));
                }
            }
            else if (orderType == OrderTypes.Delivery)
                BackupDeliveryOrder(orderDto, order);
            else if (orderType == OrderTypes.DineIn)
                BackupDineInOrder(orderDto, order);
        }

        return Ok();
    }
    private async Task<List<string>> GetBranchDetails(OrderDto orderDto)
    {
        List<string> branchDetails = new();

        if (branchDetails.Count <= 0)
            branchDetails = await GetLogoPath(orderDto.BranchId);
        return branchDetails;
    }
    private static Orders BackupMainOrderDetails(OrderDto OrderDto)
    {
        return new Orders
        {
            OrderID = OrderDto.OrderId,
            BranchID = OrderDto.BranchId,
            BranchName = OrderDto.BranchName,
            CashierID = OrderDto.CashierId,
            CashierName = OrderDto.CashierName,
            OrderDate = OrderDto.OrderDate,
            PaymentMethod = OrderDto.PaymentMethod,
            OrderState = OrderStates.Completed,
            Discount = OrderDto.TotalOrderDiscount,
            DiscountByName = OrderDto.DiscountByName,
            DiscountBy = OrderDto.DiscountBy,
            Paid = OrderDto.Paid,
            Subtotal = OrderDto.SubTotal,
            Service = OrderDto.Services,
            Tax = OrderDto.Tax,
            GrandTotal = OrderDto.GrandTotal,
            OrderNotice = OrderDto.OrderNotice,
            OrderDetails = OrderDto.OrderDetails?.Select(detail => new OrderItemsDetails
            {
                MenuSalesItemId = detail.Id,
                Quantity = detail.Quantity,
                TotalAmount = detail.TotalAmount,
                Discount = detail.HasDiscount,
                TotalDiscountPercentage = detail.DiscountPercentage ?? 0M,
                TotalDiscountAmount = detail.DiscountAmount ?? 0M,
                OrderType = OrderTypes.TakeAway,
                TotalDiscountPrice = detail.TotalDiscountPrice ?? 0M,
                TotalAfterDiscount = detail.TotalAfterDiscount,
                IsVoided = false,
                VoidAmount = 0,
                OrderItemAttributes = detail.Attributes?
                .Where(a => a.Id < 5000)
                .Select(a => new OrderItemAttributes
                {
                    OrderItemId = detail.Id,
                    AttributeItemId = a.Id,
                    AttributeName = a.Name ?? string.Empty
                })
                .ToList() ?? new List<OrderItemAttributes>()
            }).ToList()
        };
    }
    private static void BackupTakeawayOrder(OrderDto OrderDto, Orders order)
    {
        order.TakeawayCustomerName = OrderDto.CustomerName;
        order.TakeawayCustomerPhone = OrderDto.CustomerPhone;
    }
    private static void BackupDineInOrder(OrderDto OrderDto, Orders order)
    {
        order.TakerID = OrderDto.TakerID;
        order.TakerName = OrderDto.TakerName;
        order.ReservationPaid = OrderDto.ReservationPaid;
        order.ReservationRemain = OrderDto.ReservationRemain;
        order.ScheduleDateTime = OrderDto.ScheduleDateTime;
        order.CustomerCount = OrderDto.CustomerCount;
    }
    private static void BackupDeliveryOrder(OrderDto OrderDto, Orders order)
    {
        order.DeliveryCompany = OrderDto.DeliveryCompany;
        order.TitleName = OrderDto.TitleName;
        order.CustomerID = OrderDto.CustomerID;
        order.CustomerName = OrderDto.CustomerName;
        order.Phone1 = OrderDto.Phone1;
        order.Phone2 = OrderDto.Phone2;
        order.HomeNum = OrderDto.HomeNum;
        order.StreetName = OrderDto.StreetName;
        order.FloorNum = OrderDto.FloorNum;
        order.ApartmentNum = OrderDto.ApartmentNum;
        order.AddressNotice = OrderDto.AddressNotice;
        order.ZoneID = OrderDto.ZoneID;
        order.ZoneName = OrderDto.ZoneName;
        order.ZoneBonus = OrderDto.ZoneBonus;
        order.DispatchID = OrderDto.DispatchID;
        order.DriverID = OrderDto.DriverID;
        order.DriverName = OrderDto.DriverName;
        order.AssignTime = OrderDto.AssignTime;
        order.BackTime = OrderDto.BackTime;
        order.WithoutDeliveryFees = OrderDto.WithoutDeliveryFees;
        order.ClosingTime = OrderDto.ClosingTime;
    }
    private async Task<List<string>> GetLogoPath(int branchId)
    {
        var branch = await _branchService.GetBranchByIdAsync(branchId)!;
        var logoFileName = branch!.ImagePath ?? string.Empty;
        var logoWidth = branch.LogoWidth;
        var logoHeight = branch.LogoHeight;

        string logoPath = Path.Combine(_webHostEnvironment.WebRootPath, "Files", "images", logoFileName);

        List<string> branchDetails = new List<string>();
        branchDetails.Add(logoPath);
        branchDetails.Add(logoWidth.ToString());
        branchDetails.Add(logoHeight.ToString());

        return branchDetails;
    }
    private async Task printTakeAwayReceipts(OrderDto takeawayOrder, Orders createdOrder, List<string> branchDetails)
    {
        var datePart = takeawayOrder.OrderDate!.Value.Date;
        var timeNow = DateTime.Now.TimeOfDay;
        var combined = datePart.Add(timeNow);

        var receipt = new Receipt()
        {
            Id = createdOrder.OrderID,
            StoreName = takeawayOrder.BranchName!,
            CashierName = takeawayOrder.CashierName!,
            ReceiptType = OrderTypes.TakeAway.ToString(),
            DateCreated = combined,
            PaymentMethod = takeawayOrder.PaymentMethod!.ToString()!,
            FooterMessage = takeawayOrder!.FooterMessage!,
            LogoPath = branchDetails[0],
            LogoWidth = int.Parse(branchDetails[1]),
            TotalAmount = takeawayOrder.GrandTotal,
            Discount = takeawayOrder.TotalOrderDiscount,
            Tax = takeawayOrder.Tax,
            Services = takeawayOrder.Services,
            SubTotal = takeawayOrder.SubTotal,
            Items = takeawayOrder.OrderDetails!
        };
        var receiptItems = receipt.Items;

        var outputPath = await CreateCashReceiptLayOut(receipt, receiptItems);

        await PrintCashReceipt(takeawayOrder, outputPath);
    }
    private async Task<string> CreateCashReceiptLayOut(Receipt receipt, List<TableItem>? receiptItems)
    {
        var document = await Task.Run(() => new ReceiptDocument(receipt, receiptItems!));
        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd_hh-mm-ss_tt");
        var outputPath = Path.Combine(_reportsFolder, $"{timestamp}-receipt.pdf");

        document.GeneratePdf(outputPath);

        return outputPath;
    }
    private async Task PrintCashReceipt(OrderDto takeawayOrder, string reportPath)
    {

        var kitchenTypeSpecs = new KitchenTypeSpecs("Customer");

        var printer = await _kitchenServices.GetKitchenWithSpecificationAsync(kitchenTypeSpecs);

        var receiptCount = takeawayOrder.OrderSettings!
                .Where(o => o.OrderType == OrderTypes.TakeAway.ToString())
                .Select(o => o.CustomerReceiptCount)
                .FirstOrDefault();

        if (printer == null || receiptCount <= 0)
            return;

        var printers = printer!.KitchenPrinters;

        var printerNamesToUse = new List<string>();

        for (int i = 1; i <= receiptCount; i++)
        {
            var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
            if (!string.IsNullOrEmpty(printerName))
            {
                printerNamesToUse.Add(printerName);
            }
        }

        foreach (var printerName in printerNamesToUse)
            await _printerServices.PrintPdfAsync(reportPath, printerName);
    }
    private async Task<string> CreateKitchenReceiptLayOut(KitchenReceipt receipt, List<TableItem>? receiptItems)
    {
        var document = await Task.Run(() => new KitchenReceiptDocument(receipt, receiptItems!));
        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd_hh-mm-ss_tt");
        var outputPath = Path.Combine(_reportsFolder, $"{timestamp}-receipt.pdf");

        document.GeneratePdf(outputPath);

        return outputPath;
    }
    private async Task PrintBackupReceipts( OrderDto Order, Orders createdOrder, string KitchenType = "Backup")
    {

        var filteredItems = Order.OrderDetails!
         .Where(i =>
             (i.PrintInBackupReceiptFromCategory == true && i.PrintInBackupReceiptFromItem != false) ||
             (i.PrintInBackupReceiptFromCategory == false && i.PrintInBackupReceiptFromItem == true) ||
             (i.PrintInBackupReceiptFromCategory == true && i.PrintInBackupReceiptFromItem == null)
         )
         .ToList();

        var receipt = new KitchenReceipt()
        {
            Id = createdOrder.OrderID,
            CashierName = Order.CashierName!,
            OrderType = OrderTypes.TakeAway.ToString(),
            DateCreated = DateTimeOffset.Now,
            Items = filteredItems,
            KitchenNote = Order.OrderNotice!,
            KitchenType = KitchenType
        };

        if (filteredItems.Count == 0)
            return;

        var outputPath = await CreateKitchenReceiptLayOut(receipt, filteredItems);

        await PrintBackupReceipt(Order, outputPath);
    }
    private async Task PrintBackupReceipt(OrderDto takeawayOrder, string reportPath)
    {

        var kitchenTypeSpecs = new KitchenTypeSpecs("Backup");

        var printer = await _kitchenServices.GetKitchenWithSpecificationAsync(kitchenTypeSpecs);

        var receiptCount = takeawayOrder.OrderSettings!
                .Where(o => o.OrderType == OrderTypes.TakeAway.ToString())
                .Select(o => o.FullKitchenReceiptCount)
                .FirstOrDefault();

        if (printer == null || receiptCount <= 0)
            return;

        var printers = printer!.KitchenPrinters;

        var printerNamesToUse = new List<string>();

        for (int i = 1; i <= receiptCount; i++)
        {
            var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
            if (!string.IsNullOrEmpty(printerName))
            {
                printerNamesToUse.Add(printerName);
            }
        }

        foreach (var printerName in printerNamesToUse)
            await _printerServices.PrintPdfAsync(reportPath, printerName);
    }

    private async Task PrintKitchenReceipts( OrderDto takeawayOrder, Orders createdOrder)
    {
        var receiptCount = takeawayOrder.OrderSettings!
            .Where(o => o.OrderType == OrderTypes.TakeAway.ToString())
            .Select(o => o.SeparateReceiptCount)
            .FirstOrDefault();

        if (receiptCount <= 0)
            return;

        var items = takeawayOrder.OrderDetails;

        if (items == null || items.Count == 0)
            return;

        var kitchens = await _kitchenServices.GetAllKitchenTypesAsync();

        var groupedItemsWithKitchenNameAndId = items
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

        foreach (var kitchenGroup in groupedItemsWithKitchenNameAndId)
        {
            var kitchenName = kitchenGroup.KitchenName;
            var kitchenItems = kitchenGroup.Items;

            var receipt = new KitchenReceipt()
            {
                Id = createdOrder.OrderID,
                CashierName = takeawayOrder.CashierName!,
                OrderType = OrderTypes.TakeAway.ToString(),
                DateCreated = DateTimeOffset.Now,
                Items = kitchenItems,
                KitchenNote = takeawayOrder.OrderNotice!,
                KitchenType = kitchenName
            };

            var outputPath = await CreateKitchenReceiptLayOut(receipt, kitchenItems);

            var printers = kitchenGroup.Printers;
            var printerNamesToUse = new List<string>();

            for (int i = 1; i <= receiptCount; i++)
            {
                var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
                if (!string.IsNullOrEmpty(printerName))
                {
                    printerNamesToUse.Add(printerName);
                }
            }

            foreach (var printerName in printerNamesToUse)
            {
                await _printerServices.PrintPdfAsync(outputPath, printerName);
            }
        }
    }
}