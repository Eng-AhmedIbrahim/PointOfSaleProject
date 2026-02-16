using POS.Core.Services.Contract.DineInOrderServices;
using POS.Contract.Dtos.OrderDtos;
using POS.Contract.Dtos.DineIn;
using Microsoft.AspNetCore.SignalR;
using POS.API.Hubs;
using System.Text.Json;
using System.Text;
using POS.API.Helpers;
using POS.Contract.Dtos.OrderDto;
using POS.Core.Services.Contract.DeliveryServices;
using POS.Core.Entities.Delivery;

namespace POS.API.Controllers;

public class OrderController : BaseApiController
{
    private readonly IOrderService _orderService;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IBranchService _branchService;
    private readonly IPrinterServices _printerServices;
    private readonly IKitchenServices _kitchenServices;
    private readonly IMapper _mapper;
    private readonly IDineInOrderService _dineInOrderService;
    private readonly string _reportsFolder;
    private readonly CallCenterSettings _callCenterSettings;
    private readonly IHubContext<DeliveryHub> _deliveryHubContext;
    private readonly IDeliveryZoneServices _deliveryZoneServices;

    public OrderController(IOrderService orderService,
        IWebHostEnvironment webHostEnvironment,
        IBranchService branchService,
        IPrinterServices printerServices,
        IKitchenServices kitchenServices,
        IMapper mapper,
        IDineInOrderService dineInOrderService,
        CallCenterSettings callCenterSettings,
        IHubContext<DeliveryHub> deliveryHubContext,
        IDeliveryZoneServices deliveryZoneServices)
    {
        _orderService = orderService;
        _webHostEnvironment = webHostEnvironment;
        _branchService = branchService;
        _printerServices = printerServices;
        _kitchenServices = kitchenServices;
        _mapper = mapper;
        _dineInOrderService = dineInOrderService;
        _callCenterSettings = callCenterSettings;
        _deliveryHubContext = deliveryHubContext;
        _deliveryZoneServices = deliveryZoneServices;
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
                    if (orderDto.SkipPrintingOnServer != true)
                    {
                        List<string> branchDetails = await GetBranchDetails(orderDto);
                        await printTakeAwayReceipts(orderDto, createdOrder, branchDetails);

                        if (orderSettings!.FullKitchenReceiptCount > 0)
                        {
                            await PrintBackupReceipts(orderDto, createdOrder);
                        }

                        if (orderSettings.SeparateReceiptCount > 0)
                        {
                            await PrintKitchenReceipts(orderDto, createdOrder);
                        }
                    }

                    return Ok(_mapper.Map<OrderDto>(createdOrder));
                }
                else
                {
                    return BadRequest(new ApiResponse(404, "Order Not Created"));
                }
            }
            else if (orderType == OrderTypes.Delivery)
            {
                if (!_callCenterSettings.IsCentralCallCenter)
                {
                    orderDto.OrderState = OrderStates.Assigned.ToString();
                    order.OrderState = OrderStates.Assigned;
                }

                BackupDeliveryOrder(orderDto, order);

                // Fetch Zone info if missing (Bonus OR Fees)
                if (order.ZoneID.HasValue)
                {
                    var zone = await _deliveryZoneServices.GetZoneByIdAsync(order.ZoneID.Value);
                    if (zone != null)
                    {
                        if (order.ZoneBonus == null || order.ZoneBonus == 0)
                        {
                            order.ZoneBonus = zone.ZoneBonus;
                            orderDto.ZoneBonus = zone.ZoneBonus;
                        }

                        if ((order.DeliveryFees == null || order.DeliveryFees == 0) && order.WithoutDeliveryFees != true)
                        {
                            order.DeliveryFees = zone.DeliveryFee;
                            orderDto.DeliveryFees = zone.DeliveryFee;
                        }
                    }
                }

                var createdOrder = await _orderService.CreateOrderAsync(order);

                if (createdOrder is null || createdOrder.Id == 0)
                    return BadRequest(new ApiResponse(404, "Order Not Created"));

                orderDto.OrderId = createdOrder.OrderID;

                if (_callCenterSettings.IsCentralCallCenter)
                {
                    var branchUrlToDispatch = orderDto.DeliveryBranchUrl ?? string.Empty;

                    if (!string.IsNullOrEmpty(branchUrlToDispatch))
                    {
                        // Add Call Center API URL so branch can send updates back
                        var callCenterApiUrl = $"{Request.Scheme}://{Request.Host}";
                        orderDto.CallCenterApiUrl = callCenterApiUrl;

                        using var httpClient = new HttpClient();
                        var json = JsonSerializer.Serialize(orderDto);
                        var content = new StringContent(json, Encoding.UTF8, "application/json");

                        try
                        {
                            var response = await httpClient.PostAsync($"{branchUrlToDispatch}/api/order/receiveDispatchedOrder", content);

                            if (response.IsSuccessStatusCode)
                            {
                                var branchResponseJson = await response.Content.ReadAsStringAsync();
                                var branchOrderDto = JsonSerializer.Deserialize<OrderDto>(branchResponseJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                
                                createdOrder.OrderState = OrderStates.SentToBranch;
                                if (branchOrderDto != null)
                                {
                                    createdOrder.CallCenterOrderId = branchOrderDto.OrderId; // Store Branch ID in CallCenterOrderId field on Central side
                                }
                                await _orderService.UpdateOrderAsync(createdOrder);

                                await _deliveryHubContext.Clients.All.SendAsync("OrderDispatchedCentralNotification", orderDto);
                                return Ok(_mapper.Map<OrderDto>(createdOrder));
                            }
                            else
                            {
                                var errorContent = await response.Content.ReadAsStringAsync();
                                createdOrder.OrderState = OrderStates.FailedToDeliverToBranch;
                                await _orderService.UpdateOrderStatusAsync(createdOrder.Id, OrderStates.FailedToDeliverToBranch);

                                await _deliveryHubContext.Clients.All.SendAsync("OrderDispatchFailedCentralNotification", orderDto, errorContent);
                                return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse(500, $"Failed to dispatch order to branch: {errorContent}"));
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Exception dispatching order {OrderId}", orderDto.OrderId);
                            createdOrder.OrderState = OrderStates.FailedToDeliverToBranch;
                            await _orderService.UpdateOrderStatusAsync(createdOrder.Id, OrderStates.FailedToDeliverToBranch);

                            await _deliveryHubContext.Clients.All.SendAsync("OrderDispatchFailedCentralNotification", orderDto, ex.Message);
                            return StatusCode(StatusCodes.Status500InternalServerError, new ApiResponse(500, $"Exception dispatching order: {ex.Message}"));
                        }
                    }
                    else
                    {
                        await _orderService.UpdateOrderStatusAsync(createdOrder.Id, OrderStates.FailedToDeliverToBranch);
                        await _deliveryHubContext.Clients.All.SendAsync("OrderDispatchFailedCentralNotification", orderDto, "No branch URL provided.");
                        return BadRequest(new ApiResponse(400, "No delivery branch URL provided."));
                    }
                }
                else
                {
                    await _deliveryHubContext.Clients.All.SendAsync("ReceiveNewDeliveryOrder", orderDto);

                    if (orderDto.SkipPrintingOnServer != true)
                    {
                        List<string> branchDetails = await GetBranchDetails(orderDto);
                        await printDeliveryReceipts(orderDto, createdOrder, branchDetails);

                        if (orderSettings!.FullKitchenReceiptCount > 0)
                            await PrintBackupReceipts(orderDto, createdOrder);

                        if (orderSettings.SeparateReceiptCount > 0)
                            await PrintKitchenReceipts(orderDto, createdOrder);
                    }

                    return Ok(_mapper.Map<OrderDto>(createdOrder));
                }
            }
            else if (orderType == OrderTypes.DineIn)
            {
                BackupDineInOrder(orderDto, order);
                
                if (orderDto.SkipPrintingOnServer != true)
                {
                    List<string> branchDetails = await GetBranchDetails(orderDto);
                    await printDineInReceipts(orderDto, branchDetails);

                    if (orderSettings!.FullKitchenReceiptCount > 0)
                    {
                        await PrintBackupReceipts(orderDto, order, isFollowUp: false, KitchenType: "Backup");
                    }

                    if (orderSettings.SeparateReceiptCount > 0)
                    {
                        await PrintKitchenReceipts(orderDto, order);
                    }
                }
                return Ok(orderDto);
            }
        }

        return Ok();
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(typeof(Orders), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [HttpPut("createOrder")] // Aligned with OrderSettingsService.UpdateOrderAsync
    public async Task<IActionResult> UpdateOrderAsync([FromBody] OrderDto orderDto)
    {
        if (orderDto == null || orderDto.OrderId == 0)
            return BadRequest("Invalid order data or OrderId.");

        var order = BackupMainOrderDetails(orderDto);
        
        if (order.OrderType == OrderTypes.Delivery)
        {
            BackupDeliveryOrder(orderDto, order);

            // Fetch Zone info if missing (Bonus OR Fees)
            if (order.ZoneID.HasValue)
            {
                var zone = await _deliveryZoneServices.GetZoneByIdAsync(order.ZoneID.Value);
                if (zone != null)
                {
                    if (order.ZoneBonus == null || order.ZoneBonus == 0)
                    {
                        order.ZoneBonus = zone.ZoneBonus;
                        orderDto.ZoneBonus = zone.ZoneBonus;
                    }

                    if ((order.DeliveryFees == null || order.DeliveryFees == 0) && order.WithoutDeliveryFees != true)
                    {
                        order.DeliveryFees = zone.DeliveryFee;
                        orderDto.DeliveryFees = zone.DeliveryFee;
                    }
                }
            }
        }
        
        // Ensure the mapped order has the correct OrderID for lookup
        order.OrderID = orderDto.OrderId; 

        var updatedOrder = await _orderService.FullUpdateOrderAsync(order);

        if (updatedOrder == null)
            return NotFound(new ApiResponse(404, "Order not found."));

        // Print receipts after update with "تابع" flag
        if (orderDto.SkipPrintingOnServer != true)
        {
            var orderSettings = orderDto.OrderSettings?.FirstOrDefault(o => o.OrderType == orderDto.OrderType);
            if (orderSettings != null)
            {
                List<string> branchDetails = await GetBranchDetails(orderDto);
                if (string.Equals(orderDto.OrderType, OrderTypes.Delivery.ToString(), StringComparison.OrdinalIgnoreCase))
                    await printDeliveryReceipts(orderDto, updatedOrder, branchDetails, isFollowUp: true);
                else
                    await printTakeAwayReceipts(orderDto, updatedOrder, branchDetails, isFollowUp: true);

                if (orderSettings.FullKitchenReceiptCount > 0)
                    await PrintBackupReceipts(orderDto, updatedOrder, isFollowUp: true);

                if (orderSettings.SeparateReceiptCount > 0)
                    await PrintKitchenReceipts(orderDto, updatedOrder, isFollowUp: true);
            }
        }

        return Ok(_mapper.Map<OrderDto>(updatedOrder));
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(typeof(Orders), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [HttpPost("receiveDispatchedOrder")]
    public async Task<IActionResult> ReceiveDispatchedOrderAsync([FromBody] OrderDto orderDto)
    {
        if (orderDto == null || orderDto.OrderDetails == null || !orderDto.OrderDetails.Any())
            return BadRequest("Invalid order data received.");

        var order = BackupMainOrderDetails(orderDto);
        BackupDeliveryOrder(orderDto, order); // Ensure delivery details are backed up
        order.CallCenterOrderId = orderDto.OrderId; // Store Call Center ID in CallCenterOrderId field on Branch side

        var createdOrder = await _orderService.CreateOrderAsync(order);

        if (createdOrder is null || createdOrder.Id == 0)
        {
            Log.Error("Failed to create dispatched order in branch local database. Order ID: {OrderId}", orderDto.OrderId);
            return BadRequest(new ApiResponse(404, "Dispatched Order Not Created in branch DB."));
        }

        orderDto.OrderId = createdOrder.OrderID;

        await _deliveryHubContext.Clients.All.SendAsync("ReceiveNewDeliveryOrder", orderDto);

        List<string> branchDetails = await GetBranchDetails(orderDto);

        await printDeliveryReceipts(orderDto, createdOrder, branchDetails);

        var orderSettings = orderDto.OrderSettings!.FirstOrDefault(o => o.OrderType == OrderTypes.Delivery.ToString());
        if (orderSettings?.FullKitchenReceiptCount > 0)
            await PrintBackupReceipts(orderDto, createdOrder);

        if (orderSettings?.SeparateReceiptCount > 0)
            await PrintKitchenReceipts(orderDto, createdOrder);

        return Ok(_mapper.Map<OrderDto>(createdOrder));
    }

    [ApiExplorerSettings(IgnoreApi = true)]
    [HttpPut("receiveOrderUpdate")]
    public async Task<IActionResult> ReceiveOrderUpdateFromBranch([FromBody] OrderDto orderDto)
    {
        if (orderDto == null || orderDto.CallCenterOrderId == null)
            return BadRequest("Invalid order update data.");

        // Find the order in call center database using CallCenterOrderId (which is the original call center order ID)
        var order = await _orderService.GetOrderByOrderIdAsync(orderDto.CallCenterOrderId.Value);

        if (order == null)
        {
            // If it has a ParentOrderId, it might be a new addition from the branch
            if (orderDto.ParentOrderId.HasValue)
            {
                Log.Information("Received new addition order from branch for Parent Order {ParentId}", orderDto.ParentOrderId);
                var additionOrder = _mapper.Map<OrderDto, Orders>(orderDto);
                additionOrder.OrderType = OrderTypes.Delivery;
                // Important: Here CallCenterOrderId on the central side will store the Branch's OrderID
                additionOrder.CallCenterOrderId = orderDto.OrderId; 
                
                var createdAddition = await _orderService.CreateOrderAsync(additionOrder);
                return Ok(_mapper.Map<OrderDto>(createdAddition));
            }

            Log.Warning("Order not found in call center database. CallCenterOrderId: {OrderId}", orderDto.CallCenterOrderId);
            return NotFound(new ApiResponse(404, "Order not found in call center database."));
        }

        // Update the order with new status
        order.OrderState = Enum.TryParse<OrderStates>(orderDto.OrderState, out var state) ? state : order.OrderState;
        order.DriverID = orderDto.DriverID ?? order.DriverID;
        order.DriverName = orderDto.DriverName ?? order.DriverName;
        order.AssignTime = orderDto.AssignTime ?? order.AssignTime;
        order.DispatchID = orderDto.DispatchID ?? order.DispatchID;
        order.ClosingTime = orderDto.ClosingTime ?? order.ClosingTime;
        
        // Sync Voiding details
        if (order.OrderState == OrderStates.Voided)
        {
            order.VoidBy = orderDto.VoidBy;
            order.VoidByName = orderDto.VoidByName;
            order.VoidReason = orderDto.VoidReason;
            order.VoidTime = orderDto.VoidTime ?? DateTime.Now;
        }

        await _orderService.UpdateOrderAsync(order);

        Log.Information("Order {OrderId} updated in call center from branch. State: {State}", order.OrderID, order.OrderState);

        return Ok(_mapper.Map<OrderDto>(order));
    }

    [HttpGet("{orderId}")]
    public async Task<ActionResult<OrderDto>> GetOrderByIdAsync(int orderId)
    {
        var order = await _orderService.GetOrderByOrderIdAsync(orderId);
        if (order == null) return NotFound();
        return Ok(_mapper.Map<OrderDto>(order));
    }

    [HttpPut("void/{orderId}")]
    public async Task<ActionResult<bool>> VoidOrder(int orderId, [FromQuery] string reason, [FromQuery] string voidBy, [FromQuery] string voidByName)
    {
        try
        {
            var order = await _orderService.GetOrderByOrderIdAsync(orderId);
            if (order == null) return NotFound();

            // Permission Check for Delivery
            if (order.OrderType == OrderTypes.Delivery)
            {
                var settings = await _orderService.GetOrderSettingAsync(OrderTypes.Delivery, order.MachineName);
                if (settings != null)
                {
                    bool canVoid = _callCenterSettings.IsCentralCallCenter 
                        ? (settings.CanVoidFromCallCenter ?? true) 
                        : (settings.CanVoidFromBranch ?? true);

                    if (!canVoid)
                        return BadRequest(new ApiResponse(403, "Voiding from this device is disabled by settings."));
                }
            }

            var result = await _orderService.VoidOrderAsync(orderId, reason, voidBy, voidByName);
            if (result)
            {
                // Sync with Call Center if needed
                if (order.OrderType == OrderTypes.Delivery)
                {
                    var orderDto = _mapper.Map<OrderDto>(order);
                    orderDto.OrderState = OrderStates.Voided.ToString();
                    orderDto.VoidReason = reason;
                    orderDto.VoidBy = voidBy;
                    orderDto.VoidByName = voidByName;
                    
                    if (!_callCenterSettings.IsCentralCallCenter)
                    {
                        await SendUpdateToCallCenter(orderDto);
                    }
                    else if (!string.IsNullOrEmpty(order.DeliveryBranchUrl))
                    {
                        // If we are at Call Center, notify the branch
                        await SendUpdateToBranch(orderDto, order.DeliveryBranchUrl, "receiveOrderUpdate");
                    }
                }

                return Ok(result);
            }
            else
                return BadRequest(new ApiResponse(400, "Failed to void order"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error voiding order {OrderId}", orderId);
            return StatusCode(500, new ApiResponse(500, "Internal server error"));
        }
    }
    
    [HttpPut("incrementPrintCount/{orderId}")]
    public async Task<ActionResult<int>> IncrementPrintCount(int orderId)
    {
        try
        {
            var result = await _orderService.IncrementPrintCountAsync(orderId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error incrementing print count for order {OrderId}", orderId);
            return StatusCode(500, new ApiResponse(500, "Internal server error"));
        }
    }
    
    [HttpPut("void-items/{orderId}")]
    public async Task<ActionResult<bool>> VoidOrderItems(int orderId, [FromBody] List<OrderItemVoidDto> itemsToVoid, [FromQuery] string reason, [FromQuery] string voidBy, [FromQuery] string voidByName)
    {
        try
        {
            var order = await _orderService.GetOrderByOrderIdAsync(orderId);
            if (order == null) return NotFound();

            // Permission Check for Delivery
            if (order.OrderType == OrderTypes.Delivery)
            {
                var settings = await _orderService.GetOrderSettingAsync(OrderTypes.Delivery, order.MachineName);
                if (settings != null)
                {
                    bool canVoid = _callCenterSettings.IsCentralCallCenter 
                        ? (settings.CanVoidFromCallCenter ?? true) 
                        : (settings.CanVoidFromBranch ?? true);

                    if (!canVoid)
                        return BadRequest(new ApiResponse(403, "Modifying items from this device is disabled by settings."));
                }
            }

            var result = await _orderService.VoidOrderItemsAsync(orderId, itemsToVoid, reason, voidBy, voidByName);
            if (result)
            {
                // Sync updated order
                var updatedOrder = await _orderService.GetOrderByOrderIdAsync(orderId);
                var orderDto = _mapper.Map<OrderDto>(updatedOrder);

                if (order.OrderType == OrderTypes.Delivery)
                {
                    if (!_callCenterSettings.IsCentralCallCenter)
                    {
                        await SendUpdateToCallCenter(orderDto);
                    }
                    else if (!string.IsNullOrEmpty(order.DeliveryBranchUrl))
                    {
                        await SendUpdateToBranch(orderDto, order.DeliveryBranchUrl, "receiveOrderUpdate");
                    }
                }

                return Ok(result);
            }
            else
                return BadRequest(new ApiResponse(400, "Failed to void items"));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error voiding items from order {OrderId}", orderId);
            return StatusCode(500, new ApiResponse(500, "Internal server error"));
        }
    }

    private async Task printDineInReceipts(OrderDto dineInOrder, List<string> branchDetails)
    {
        // Try to find the order to check print count
        var currentPrintCount = await _dineInOrderService.IncrementPrintCountAsync(dineInOrder.OrderId);
        bool isCopy = currentPrintCount > 0;

        var receipt = new Receipt()
        {
            Id = dineInOrder.OrderId,
            StoreName = dineInOrder.BranchName!,
            CashierName = dineInOrder.CashierName!,
            ReceiptType = OrderTypes.DineIn.ToString(),
            DateCreated = DateTime.Now,
            PaymentMethod = dineInOrder.PaymentMethod?.ToString() ?? "N/A",
            FooterMessage = dineInOrder.FooterMessage ?? "",
            LogoPath = branchDetails[0],
            LogoWidth = int.Parse(branchDetails[1]),
            TotalAmount = dineInOrder.GrandTotal,
            Discount = dineInOrder.TotalOrderDiscount,
            Tax = dineInOrder.Tax,
            Services = dineInOrder.Services,
            SubTotal = dineInOrder.SubTotal,
            Items = dineInOrder.OrderDetails!,
            TableId = dineInOrder.TableId,
            TableName = dineInOrder.TableName,
            WaiterName = dineInOrder.WaiterName,
            IsCopy = isCopy
        };

        var outputPath = await CreateCashReceiptLayOut(receipt, receipt.Items);
        await PrintDineInReceipt(dineInOrder, outputPath);
    }

    private async Task PrintDineInReceipt(OrderDto dineInOrder, string reportPath)
    {
        var kitchens = await _kitchenServices.GetAllKitchenTypesAsync();
        var printer = kitchens?.ElementAtOrDefault(0);

        var receiptCount = dineInOrder.OrderSettings!
                .Where(o => o.OrderType == OrderTypes.DineIn.ToString())
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
    private async Task<List<string>> GetBranchDetails(OrderDto orderDto)
    {
        List<string> branchDetails = new();

        if (branchDetails.Count <= 0)
            branchDetails = await GetLogoPath(orderDto.BranchId);
        return branchDetails;
    }
    private static Orders BackupMainOrderDetails(OrderDto OrderDto)
    {
        Enum.TryParse<OrderTypes>(OrderDto.OrderType ?? "TakeAway", out var orderType);
        
        return new Orders
        {
            OrderID = OrderDto.OrderId,
            BranchID = OrderDto.BranchId,
            BranchName = OrderDto.BranchName,
            CashierID = OrderDto.CashierId,
            CashierName = OrderDto.CashierName,
            OrderDate = OrderDto.OrderDate,
            PaymentMethod = OrderDto.PaymentMethod,
            OrderState = Enum.TryParse<OrderStates>(OrderDto.OrderState, out var state) ? state : (orderType == OrderTypes.DineIn ? OrderStates.Pending : OrderStates.Completed),
            Discount = OrderDto.TotalOrderDiscount,
            DiscountByName = OrderDto.DiscountByName,
            DiscountReason = OrderDto.DiscountReason,
            DiscountType = OrderDto.DiscountType,
            DiscountPercentage = OrderDto.DiscountPercentage,
            TotalDiscount = OrderDto.TotalDiscount,
            TotalVoid = OrderDto.TotalVoid,
            VoidCount = OrderDto.VoidCount,
            VoidAmount = OrderDto.VoidAmount,
            Paid = OrderDto.Paid,
            Remain = OrderDto.Remaining,  // ✅ إضافة حفظ المبلغ المتبقي
            Subtotal = OrderDto.SubTotal,
            Service = OrderDto.Services,
            Tax = OrderDto.Tax,
            GrandTotal = OrderDto.GrandTotal,
            OrderNotice = OrderDto.OrderNotice,
            MachineName = OrderDto.MachineName,
            TableID = OrderDto.TableId,
            TableName = OrderDto.TableName,
            OrderType = orderType,
            CallCenterOrderId = OrderDto.CallCenterOrderId,
            OrderDetails = OrderDto.OrderDetails?.Select(detail => new OrderItemsDetails
            {
                MenuSalesItemId = detail.Id,
                Quantity = detail.Quantity,
                TotalAmount = detail.TotalAmount,
                Discount = detail.HasDiscount,
                TotalDiscountPercentage = detail.DiscountPercentage ?? 0M,
                TotalDiscountAmount = detail.DiscountAmount ?? 0M,
                OrderType = orderType,
                ItemName = detail.Name,
                ItemNameAr = detail.NameAr,
                ItemKitchenTypeId = detail.ItemKitchenTypeId,
                CategoryKitchenTypeId = detail.CategoryKitchenTypeId,
                PrintInBackupReceiptFromCategory = detail.PrintInBackupReceiptFromCategory,
                PrintInBackupReceiptFromItem = detail.PrintInBackupReceiptFromItem,
                TotalDiscountPrice = detail.TotalDiscountPrice ?? 0M,
                TotalAfterDiscount = detail.TotalAfterDiscount,
                IsVoided = detail.IsVoided,
                VoidAmount = detail.VoidAmount ?? 0,
                TotalVoidAmount = detail.TotalVoidAmount ?? 0,
                VoidBy = detail.VoidBy,
                VoidByName = detail.VoidByName,
                VoidTime = detail.VoidTime,
                VoidReason = detail.VoidReason,
                UnitPrice = detail.Price,  // ✅ إضافة حفظ سعر الوحدة
                CategoryId = detail.CategoryId,  // ✅ إضافة حفظ معرف الفئة
                CategoryName = detail.CategoryName,  // ✅ إضافة حفظ اسم الفئة
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
        //order.TakerID = OrderDto.TakerID;
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
        
        // Added missing delivery fields
        order.TakerID = OrderDto.TakerID;
        order.TakerName = OrderDto.TakerName;
        order.DeliveryFees = OrderDto.DeliveryFees;
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
    private async Task printTakeAwayReceipts(OrderDto takeawayOrder, Orders createdOrder, List<string> branchDetails, bool isFollowUp = false)
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
            Items = takeawayOrder.OrderDetails!,
            IsFollowUp = isFollowUp
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

        var kitchens = await _kitchenServices.GetAllKitchenTypesAsync();
        var printer = kitchens?.ElementAtOrDefault(0);

        var receiptCount = takeawayOrder.OrderSettings!
                .Where(o => o.OrderType == (takeawayOrder.OrderType ?? OrderTypes.TakeAway.ToString()))
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
    private async Task PrintBackupReceipts( OrderDto Order, Orders createdOrder, bool isFollowUp = false, string KitchenType = "Backup")
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
            OrderType = Order.OrderType ?? OrderTypes.TakeAway.ToString(),
            DateCreated = DateTimeOffset.Now,
            Items = filteredItems,
            KitchenNote = Order.OrderNotice!,
            KitchenType = KitchenType,
            TableId = Order.TableId,
            TableName = Order.TableName,
            IsFollowUp = isFollowUp
        };

        if (filteredItems.Count == 0)
            return;

        var outputPath = await CreateKitchenReceiptLayOut(receipt, filteredItems);

        await PrintBackupReceipt(Order, outputPath);
    }
    private async Task PrintBackupReceipt(OrderDto takeawayOrder, string reportPath)
    {

        var kitchens = await _kitchenServices.GetAllKitchenTypesAsync();
        var printer = kitchens?.ElementAtOrDefault(1);

        var receiptCount = takeawayOrder.OrderSettings!
                .Where(o => o.OrderType == (takeawayOrder.OrderType ?? OrderTypes.TakeAway.ToString()))
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

    private async Task PrintKitchenReceipts( OrderDto takeawayOrder, Orders createdOrder, bool isFollowUp = false)
    {
        var receiptCount = takeawayOrder.OrderSettings!
            .Where(o => o.OrderType == (takeawayOrder.OrderType ?? OrderTypes.TakeAway.ToString()))
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
                OrderType = takeawayOrder.OrderType ?? OrderTypes.TakeAway.ToString(),
                DateCreated = DateTimeOffset.Now,
                Items = kitchenItems,
                KitchenNote = takeawayOrder.OrderNotice!,
                KitchenType = kitchenName,
                TableId = takeawayOrder.TableId,
                TableName = takeawayOrder.TableName,
                IsFollowUp = isFollowUp
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

    [HttpPost("add-items")]
    public async Task<IActionResult> AddItemsToOrder([FromBody] OrderDto orderDto)
    {
        if (orderDto == null || !orderDto.ParentOrderId.HasValue)
            return BadRequest(new ApiResponse(400, "Parent Order ID is required for additional items."));

        // Get parent order
        var parentOrder = await _orderService.GetOrderByOrderIdAsync(orderDto.ParentOrderId.Value);
        if (parentOrder == null) return NotFound(new ApiResponse(404, "Parent order not found."));

        // Permission Check
        var settings = await _orderService.GetOrderSettingAsync(OrderTypes.Delivery, orderDto.MachineName);
        if (settings != null)
        {
            bool canAdd = _callCenterSettings.IsCentralCallCenter 
                ? (settings.CanAddItemsFromCallCenter ?? true) 
                : (settings.CanAddItemsFromBranch ?? true);

            if (!canAdd)
                return BadRequest(new ApiResponse(403, "Adding items from this device is disabled by settings."));
        }

        // Create the addition as a new linked order
        var orderItems = _mapper.Map<List<TableItem>, List<OrderItemsDetails>>(orderDto.OrderDetails!);
        var order = _mapper.Map<OrderDto, Orders>(orderDto);
        order.OrderDetails = orderItems;
        order.OrderType = OrderTypes.Delivery;
        order.ParentOrderId = orderDto.ParentOrderId; // Link to parent
        order.DeliveryBranchUrl = parentOrder.DeliveryBranchUrl;
        
        var createdOrder = await _orderService.CreateOrderAsync(order);
        if (createdOrder == null) return BadRequest(new ApiResponse(500, "Failed to create addition order."));

        var resultDto = _mapper.Map<OrderDto>(createdOrder);
        // Ensure CallCenterApiUrl is carried over for sync
        resultDto.CallCenterApiUrl = parentOrder.CallCenterApiUrl ?? orderDto.CallCenterApiUrl;

        // Sync logic
        if (_callCenterSettings.IsCentralCallCenter)
        {
            // If at Call Center, send to the same branch as parent
            if (!string.IsNullOrEmpty(parentOrder.DeliveryBranchUrl))
            {
                await SendUpdateToBranch(resultDto, parentOrder.DeliveryBranchUrl, "receiveDispatchedOrder");
            }
        }
        else
        {
            // If at Branch, notify Call Center
            await SendUpdateToCallCenter(resultDto);
        }

        // Print Kitchen ONLY for new items
        var orderSettings = await _orderService.GetOrderSettingsAsync(orderDto.MachineName);
        resultDto.OrderSettings = _mapper.Map<IReadOnlyList<OrderSetting>, ICollection<OrderSettingToReturnDto>>(orderSettings);
        
        if (resultDto.OrderSettings.Any(s => s.SeparateReceiptCount > 0))
            await PrintKitchenReceipts(resultDto, createdOrder, isFollowUp: true);

        // Also print Customer and Backup receipts for the added items
        if (orderDto.SkipPrintingOnServer != true)
        {
            List<string> branchDetails = await GetBranchDetails(orderDto);
            
            if (parentOrder.OrderType == OrderTypes.Delivery)
                 await printDeliveryReceipts(resultDto, createdOrder, branchDetails, isFollowUp: true, parentDeliveryFees: parentOrder.DeliveryFees);
            else
                 await printTakeAwayReceipts(resultDto, createdOrder, branchDetails, isFollowUp: true);

            if (resultDto.OrderSettings.Any(s => s.FullKitchenReceiptCount > 0))
                 await PrintBackupReceipts(resultDto, createdOrder, isFollowUp: true, KitchenType: "Backup");
        }

        return Ok(resultDto);
    }

    private async Task SendUpdateToCallCenter(OrderDto orderDto)
    {
        // Allow if it has CallCenterOrderId OR if it is a new addition (has ParentOrderId)
        if (string.IsNullOrEmpty(orderDto.CallCenterApiUrl)) return;
        if (!orderDto.CallCenterOrderId.HasValue && !orderDto.ParentOrderId.HasValue) return;

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var json = JsonSerializer.Serialize(orderDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await httpClient.PutAsync($"{orderDto.CallCenterApiUrl}/api/order/receiveOrderUpdate", content);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error syncing update to Call Center for order {OrderId}", orderDto.OrderId);
        }
    }

    private async Task SendUpdateToBranch(OrderDto orderDto, string branchUrl, string endpoint)
    {
        if (string.IsNullOrEmpty(branchUrl)) return;

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);
            var json = JsonSerializer.Serialize(orderDto);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await httpClient.PostAsync($"{branchUrl}/api/order/{endpoint}", content);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error syncing update to Branch {BranchUrl} for order {OrderId}", branchUrl, orderDto.OrderId);
        }
    }

    private async Task printDeliveryReceipts(OrderDto deliveryOrder, Orders createdOrder, List<string> branchDetails, bool isFollowUp = false, decimal? parentDeliveryFees = null)
    {
        var datePart = deliveryOrder.OrderDate!.Value.Date;
        var timeNow = DateTime.Now.TimeOfDay;
        var combined = datePart.Add(timeNow);

        var receipt = new DeliveryReceipt()
        {
            Id = deliveryOrder.ParentOrderId ?? createdOrder.OrderID,
            ParentOrderId = deliveryOrder.ParentOrderId,
            StoreName = deliveryOrder.BranchName!,
            CashierName = deliveryOrder.CashierName!,
            ReceiptType = OrderTypes.Delivery.ToString(),
            DateCreated = combined,
            PaymentMethod = deliveryOrder.PaymentMethod!.ToString()!,
            FooterMessage = deliveryOrder!.FooterMessage!,
            LogoPath = branchDetails[0],
            LogoWidth = int.Parse(branchDetails[1]),
            TotalAmount = createdOrder.Subtotal ?? deliveryOrder.SubTotal,
            DeliveryFees = (createdOrder.DeliveryFees ?? deliveryOrder.DeliveryFees ?? 0) != 0 
                            ? (createdOrder.DeliveryFees ?? deliveryOrder.DeliveryFees ?? 0) 
                            : (parentDeliveryFees ?? 0),
            TotalOrder = createdOrder.GrandTotal ?? deliveryOrder.GrandTotal ?? 0,
            CustomerName = deliveryOrder.CustomerName,
            CustomerFirstPhone = deliveryOrder.Phone1,
            CustomerSecondPhone = deliveryOrder.Phone2,
            Building = deliveryOrder.HomeNum,
            FloorNumber = deliveryOrder.FloorNum,
            FlatNumber = deliveryOrder.ApartmentNum,
            ZoneName = deliveryOrder.ZoneName,
            AddressNote = deliveryOrder.AddressNotice,
            DeliveryName = deliveryOrder.DriverName,
            CustomerAddress = deliveryOrder.StreetName,
            IsFollowUp = isFollowUp
        };

        var deliveryItems = deliveryOrder.OrderDetails!;

        var outputPath = await CreateDeliveryReceiptLayOut(receipt, deliveryItems);

        await PrintCashReceipt(deliveryOrder, outputPath);
    }

    private async Task<string> CreateDeliveryReceiptLayOut(DeliveryReceipt receipt, List<TableItem>? receiptItems)
    {
        var document = await Task.Run(() => new DeliveryReceiptDocument(receipt, receiptItems!));
        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd_hh-mm-ss_tt");
        var outputPath = Path.Combine(_reportsFolder, $"{timestamp}-delivery-receipt.pdf");

        document.GeneratePdf(outputPath);

        return outputPath;
    }
}
