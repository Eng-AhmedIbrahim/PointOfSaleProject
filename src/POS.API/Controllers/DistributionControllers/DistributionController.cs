using POS.Contract.Dtos.DineIn;

namespace POS.API.Controllers.DistributionControllers;

public class DistributionController : BaseApiController
{
    private readonly IDistributionServices _distributionService;
    private readonly IMapper _mapper;
    private readonly ILogger<DistributionController> _logger;
    private readonly IDeliveryOrderApiServices _deliveryOrderApiServices;
    private readonly IOrderApiServices _orderApiServices;
    private readonly AppDbContext _dbContext;
    private readonly IOrderService _orderService;
    private readonly IHubContext<DeliveryHub> _hubContext;


    public DistributionController(IDistributionServices distributionService,
        IMapper mapper,
        ILogger<DistributionController> logger,
        IDeliveryOrderApiServices deliveryOrderApiServices,
        IOrderApiServices orderApiServices,
        IOrderService orderService,
        AppDbContext dbContext,
        IHubContext<DeliveryHub> hubContext
        )
    {

        _distributionService = distributionService;
        _mapper = mapper;
        _logger = logger;
        _deliveryOrderApiServices = deliveryOrderApiServices;
        _orderApiServices = orderApiServices;
        _orderService = orderService;
        _dbContext = dbContext;
        _hubContext = hubContext;
    }

    [HttpGet("GetUnCompletedDeliveryOrders")]
    public async Task<IActionResult> GetUnCompletedDeliveryOrders()
    {
        var orders = await _distributionService.GetUnCompletedDeliveryOrdersAsync();

        if (orders is null || !orders.Any())
            return NotFound(new ApiResponse(404, "No uncompleted delivery orders found."));

        var orderDtos = _mapper.Map<ICollection<Orders>, ICollection<OrderDto>>(orders);

        return Ok(orderDtos);
    }

    [HttpPut("dispatch")]
    public async Task<IActionResult> DispatchOrder([FromBody] OrderDto orderDto)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var result = await _distributionService.UpdateDeliveryOrdersAfterDispatchAsync(orderDto);

            if (!result)
            {
                await transaction.RollbackAsync();
                return BadRequest(new ApiResponse(500, "Failed to update order."));
            }

            if (!orderDto?.SkipPrintingOnServer ?? false )
            {
                await PrintCustomerCloseDeliveryReceipt(
                    orderDto!,
                    null!,
                    orderDto!.OrderSettings!.FirstOrDefault(o => o.OrderType == OrderTypes.Delivery.ToString())
                );
            }

            await transaction.CommitAsync();
            
            // Notify all clients via SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveOrderDispatched", orderDto);

            // Fetch the updated order from DB to ensure DTO has all calculated fields (like DriverName if it was fetched from a driver table, etc.)
            var updatedOrder = await _orderService.GetOrderByIdAsync(orderDto.Id);
            if (updatedOrder != null)
            {
                var syncDto = _mapper.Map<OrderDto>(updatedOrder);
                // Ensure sync fields are preserved
                syncDto.CallCenterApiUrl = orderDto.CallCenterApiUrl;
                syncDto.CallCenterOrderId = orderDto.CallCenterOrderId;
                await SendUpdateToCallCenter(syncDto);
            }

            return Ok(orderDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to dispatch order and print receipt.");
            return StatusCode(500, new ApiResponse(500, "An error occurred while dispatching order."));
        }
    }

    private async Task PrintCustomerCloseDeliveryReceipt(OrderDto orderDto, Orders createdOrder, OrderSettingToReturnDto? orderSettings)
    {
        List<string> branchDetails = await _orderApiServices.GetBranchDetails(orderDto);

        var printResult = await _deliveryOrderApiServices.GenerateAndPrintDeliveryReceipts(orderDto, createdOrder, branchDetails, true);
    }

    [HttpPut("collect-delivery")]
    public async Task<IActionResult> CollectDeliveryOrder([FromBody] OrderDto orderDto)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderDto.Id);
            if (order == null)
            {
                return NotFound(new ApiResponse(404, "Order not found."));
            }

            if (order.OrderState != OrderStates.Voided)
            {
                order.OrderState = (order.VoidAmount ?? 0) > 0 ? OrderStates.Voided : OrderStates.Completed;
            }
            order.BackTime = orderDto.BackTime ?? DateTime.Now;
            order.ClosingTime = DateTime.Now;
            order.CollectorID = orderDto.CollectorID;
            order.CollectorName = orderDto.CollectorName;

            _dbContext.Orders.Update(order);
            await _dbContext.SaveChangesAsync();

            await transaction.CommitAsync();

            // Update orderDto for SignalR
            orderDto.OrderState = order.OrderState.ToString();
            orderDto.ClosingTime = order.ClosingTime;
            orderDto.BackTime = order.BackTime;
            orderDto.CollectorID = order.CollectorID;
            orderDto.CollectorName = order.CollectorName;

            // Notify all clients via SignalR
            await _hubContext.Clients.All.SendAsync("ReceiveOrderCollected", orderDto);

            // Send update to Call Center if this is a branch (using refreshed data)
            var syncDto = _mapper.Map<OrderDto>(order);
            syncDto.CallCenterApiUrl = orderDto.CallCenterApiUrl;
            syncDto.CallCenterOrderId = orderDto.CallCenterOrderId;
            await SendUpdateToCallCenter(syncDto);

            return Ok(orderDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed to Collect delivery order receipt.");
            return StatusCode(500, new ApiResponse(500, "An error occurred while Collecting Delivery order."));
        }
    }

    [HttpPut("un-dispatch/{id}")]
    public async Task<IActionResult> UnDispatchOrder(int id)
    {
        var result = await _distributionService.UnDispatchOrderAsync(id);

        if (!result)
            return BadRequest(new ApiResponse(500, "Failed to un-dispatch order."));

        // Notify via SignalR (we need minimal order info or just the ID)
        await _hubContext.Clients.All.SendAsync("ReceiveOrderUnDispatched", id);

        // Fetch order to sync with Call Center
        var order = await _orderService.GetOrderByIdAsync(id);
        if (order != null)
        {
            var orderDto = _mapper.Map<OrderDto>(order);
            await SendUpdateToCallCenter(orderDto);
        }

        return Ok(true);
    }

    [HttpPut("collect-driver-orders/{driverId}")]
    public async Task<IActionResult> CollectDriverOrders(string driverId)
    {
        var orders = await _distributionService.GetOrdersByDriverAsync(driverId);
        
        foreach (var order in orders)
        {
            if (order.OrderState != OrderStates.Voided)
            {
                order.OrderState = (order.VoidAmount ?? 0) > 0 ? OrderStates.Voided : OrderStates.Completed;
            }
            order.ClosingTime = DateTime.Now;
            _dbContext.Orders.Update(order);
        }

        await _dbContext.SaveChangesAsync();

        foreach (var order in orders)
        {
            var orderDto = _mapper.Map<OrderDto>(order);
            await _hubContext.Clients.All.SendAsync("ReceiveOrderCollected", orderDto);
            await SendUpdateToCallCenter(orderDto);
        }

        return Ok(true);
    }

    [HttpPut("collect-all")]
    public async Task<IActionResult> CollectAllOrders()
    {
        var orders = await _distributionService.GetUnCompletedDeliveryOrdersAsync();
        var dispatchedOrders = orders.Where(o => o.OrderState == OrderStates.Dispatched).ToList();

        foreach (var order in dispatchedOrders)
        {
            if (order.OrderState != OrderStates.Voided)
            {
                order.OrderState = (order.VoidAmount ?? 0) > 0 ? OrderStates.Voided : OrderStates.Completed;
            }
            order.ClosingTime = DateTime.Now;
            _dbContext.Orders.Update(order);
        }

        await _dbContext.SaveChangesAsync();

        foreach (var order in dispatchedOrders)
        {
            var orderDto = _mapper.Map<OrderDto>(order);
            await _hubContext.Clients.All.SendAsync("ReceiveOrderCollected", orderDto);
            await SendUpdateToCallCenter(orderDto);
        }

        return Ok(true);
    }

    private async Task SendUpdateToCallCenter(OrderDto orderDto)
    {
        // Only send update if this order came from Call Center (has CallCenterApiUrl and CallCenterOrderId)
        if (string.IsNullOrEmpty(orderDto.CallCenterApiUrl) || !orderDto.CallCenterOrderId.HasValue)
        {
            _logger.LogDebug("Order {OrderId} does not have CallCenterApiUrl or CallCenterOrderId. Skipping sync.", orderDto.OrderId);
            return;
        }

        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            var json = System.Text.Json.JsonSerializer.Serialize(orderDto);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await httpClient.PutAsync($"{orderDto.CallCenterApiUrl}/api/order/receiveOrderUpdate", content);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Order {OrderId} update sent to Call Center successfully. State: {State}", 
                    orderDto.OrderId, orderDto.OrderState);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to send order update to Call Center. Status: {StatusCode}, Error: {Error}", 
                    response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while sending order update to Call Center for order {OrderId}", orderDto.OrderId);
        }
    }

    [HttpGet("driver-settlement")]
    public async Task<ActionResult<List<DriverSettlementDto>>> GetDriverSettlement([FromQuery] DateTime posDate)
    {
        var settlement = await _distributionService.GetDriverSettlementAsync(posDate);
        return Ok(settlement);
    }

   

    [HttpGet("voided-orders")]
    public async Task<ActionResult<List<OrderDto>>> GetVoidedOrders([FromQuery] DateTime posDate)
    {
        var orders = await _distributionService.GetVoidedDeliveryOrdersAsync(posDate);
        return Ok(_mapper.Map<List<OrderDto>>(orders));
    }
}
