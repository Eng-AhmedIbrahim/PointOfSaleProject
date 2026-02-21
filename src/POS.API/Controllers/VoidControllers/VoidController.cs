using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using POS.API.Hubs;
using POS.Contract.Dtos.DineIn;
using POS.Contract.Dtos.OrderDtos;
using POS.Contract.Dtos.VoidDtos;
using POS.Core.Entities.OrderEntity;
using POS.Core.Services.Contract.VoidServices;
using POS.Core.Services.Contract.OrderServices;
using POS.Reports.Models.Kitchen;
using POS.Reports.ReportsMakerServices;
using QuestPDF.Fluent;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Serilog;
using POS.Reports.Models;
using POS.Contract.Dtos.OrderDto;

namespace POS.API.Controllers.VoidControllers;

public class VoidController : BaseApiController
{
    private readonly IVoidService _voidService;
    private readonly IOrderService _orderService;
    private readonly IMapper _mapper;
    private readonly IKitchenServices _kitchenServices;
    private readonly IPrinterServices _printerServices;
    private readonly CallCenterSettings _callCenterSettings;
    private readonly string _reportsFolder;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public VoidController(IVoidService voidService,
        IOrderService orderService,
        IMapper mapper,
        IKitchenServices kitchenServices,
        IPrinterServices printerServices,
        CallCenterSettings callCenterSettings,
        IWebHostEnvironment webHostEnvironment)
    {
        _voidService = voidService;
        _orderService = orderService;
        _mapper = mapper;
        _kitchenServices = kitchenServices;
        _printerServices = printerServices;
        _callCenterSettings = callCenterSettings;
        _webHostEnvironment = webHostEnvironment;
        _reportsFolder = Path.Combine(_webHostEnvironment.ContentRootPath, "Reports");
    }

    [HttpDelete("voidOrder/{id}")]
    public async Task<ActionResult<bool>> VoidOrder(int id,
        [FromQuery] string reason, 
        [FromQuery] string voidBy, 
        [FromQuery] string voidByName)
    {
        var result = await _voidService.VoidOrderAsync(id, reason, voidBy, voidByName);
        
        if (result)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order != null)
            {
                var orderDto = _mapper.Map<OrderDto>(order);
                
                // 1. Kitchen Printing - ONLY if NOT in Central Call Center
                // If in CC, the branch will handle the printing when it receives the sync
                if (!_callCenterSettings.IsCentralCallCenter)
                {
                    await PrintVoidToKitchen(orderDto, order.OrderDetails?.ToList());
                }

                // 2. Sync to Call Center / Branch
                await SyncVoidUpdate(orderDto);
            }
        }
        
        return Ok(result);
    }

    [HttpPost("voidItems/{id}")]
    public async Task<ActionResult<bool>> VoidItems(int id, 
        [FromBody] List<OrderItemVoidDto> itemsToVoid, 
        [FromQuery] string reason, 
        [FromQuery] string voidBy, 
        [FromQuery] string voidByName)
    {
        var result = await _voidService.VoidItemsAsync(id, itemsToVoid, reason, voidBy, voidByName);
        
        if (result)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order != null)
            {
                var orderDto = _mapper.Map<OrderDto>(order);
                
                // Identify items that were just voided to print them
                var voidedDetailIds = itemsToVoid.Select(i => i.OrderItemDetailId).ToList();
                var itemsForKitchen = order.OrderDetails?
                    .Where(d => voidedDetailIds.Contains(d.Id))
                    .ToList();

                // 1. Kitchen Printing - ONLY if NOT in Central Call Center
                if (!_callCenterSettings.IsCentralCallCenter && itemsForKitchen != null && itemsForKitchen.Any())
                {
                    await PrintVoidToKitchen(orderDto, itemsForKitchen);
                }

                // 2. Sync to Call Center / Branch
                await SyncVoidUpdate(orderDto);
            }
        }
        
        return Ok(result);
    }

    [HttpGet("report")]
    public async Task<ActionResult<List<VoidReportDto>>> GetVoidReport([FromQuery] DateTime posDate)
    {
        var report = await _voidService.GetVoidReportAsync(posDate);
        return Ok(report);
    }

    private async Task SyncVoidUpdate(OrderDto orderDto)
    {
        if (_callCenterSettings.IsCentralCallCenter)
        {
            // If we are in CC, notify the branch
            if (!string.IsNullOrEmpty(orderDto.DeliveryBranchUrl))
            {
                await SendUpdateToBranch(orderDto, orderDto.DeliveryBranchUrl, "receiveUpdateFromCallCenter");
            }
        }
        else
        {
            // If we are in Branch, notify CC
            await SendUpdateToCallCenter(orderDto);
        }
    }

    private async Task SendUpdateToCallCenter(OrderDto orderDto)
    {
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
            Log.Error(ex, "Error syncing void update to Call Center for order {OrderId}", orderDto.Id);
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
            await httpClient.PutAsync($"{branchUrl}/api/order/{endpoint}", content);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error syncing void update to Branch {BranchUrl} for order {OrderId}", branchUrl, orderDto.OrderId);
        }
    }

    // Helper method that can be called internally or potentially publicly if moved to a service
    private async Task PrintVoidToKitchen(OrderDto orderDto, List<OrderItemsDetails>? itemsToVoid)
    {
        try 
        {
            if (itemsToVoid == null || !itemsToVoid.Any()) return;

            var kitchens = await _kitchenServices.GetAllKitchenTypesAsync();
            var kitchenItems = _mapper.Map<List<OrderItemsDetails>, List<TableItem>>(itemsToVoid);

            // Group by kitchen
            var groupedItems = kitchenItems
                .Where(item => item.ItemKitchenTypeId.HasValue || item.CategoryKitchenTypeId.HasValue)
                .GroupBy(item => item.ItemKitchenTypeId ?? item.CategoryKitchenTypeId!.Value)
                .Join(kitchens,
                      g => g.Key,
                      k => k.Id,
                      (g, k) => new
                      {
                          KitchenName = k.KitchenName ?? $"Kitchen_{k.Id}",
                          Items = g.ToList(),
                          Printers = k.KitchenPrinters
                      });

            foreach (var group in groupedItems)
            {
                var receipt = new KitchenReceipt()
                {
                    Id = orderDto.OrderId,
                    CashierName = orderDto.CashierName ?? "System",
                    OrderType = orderDto.OrderType ?? "Unknown",
                    DateCreated = DateTimeOffset.Now,
                    Items = group.Items,
                    KitchenNote = $"*** VOID/ملغي ***\nReason: {orderDto.VoidReason}",
                    KitchenType = group.KitchenName,
                    TableId = orderDto.TableId,
                    TableName = orderDto.TableName,
                    IsVoid = true
                };

                var document = new KitchenReceiptDocument(receipt, group.Items);
                var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss_fff");
                var outputPath = Path.Combine(_reportsFolder, $"VOID_{timestamp}_{receipt.Id}.pdf");
                
                Directory.CreateDirectory(_reportsFolder);
                document.GeneratePdf(outputPath);

                var printers = group.Printers;
                for (int i = 1; i <= 3; i++)
                {
                    var printerName = typeof(KitchenPrinters).GetProperty($"Copy{i}")?.GetValue(printers) as string;
                    if (!string.IsNullOrEmpty(printerName))
                    {
                        await _printerServices.PrintPdfAsync(outputPath, printerName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error printing void to kitchen for order {OrderId}", orderDto.OrderId);
        }
    }
}
