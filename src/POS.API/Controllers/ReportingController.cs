using Microsoft.AspNetCore.Mvc;
using POS.API.Errors;
using global::POS.Contract.Dtos.ReportingDtos;
using global::POS.Contract.Dtos.DineIn;
using POS.Core.Services.Contract.ReportingServices;

namespace POS.API.Controllers;

public class ReportingController : BaseApiController
{
    private readonly IReportingService _reportingService;

    public ReportingController(IReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("sales-summary")]
    public async Task<ActionResult<SalesSummaryDto>> GetSalesSummary([FromQuery] DateTime posDate)
    {
        var summary = await _reportingService.GetSalesSummaryAsync(posDate);
        return Ok(summary);
    }

    [HttpGet("accounts-summary")]
    public async Task<ActionResult<List<AccountSummaryDto>>> GetAccountsSummary([FromQuery] DateTime posDate, [FromQuery] string staffType)
    {
        var summary = await _reportingService.GetAccountsSummaryAsync(posDate, staffType);
        return Ok(summary);
    }

    [HttpGet("today-orders")]
    public async Task<ActionResult<List<OrderDto>>> GetTodayOrders([FromQuery] DateTime posDate, [FromQuery] string? orderType = null)
    {
        var orders = await _reportingService.GetTodayOrdersAsync(posDate, orderType);
        return Ok(orders);
    }

    [HttpGet("sales-items-summary")]
    public async Task<ActionResult<List<SalesItemSummaryDto>>> GetSalesItemsSummary([FromQuery] DateTime posDate)
    {
        var summary = await _reportingService.GetSalesItemsSummaryAsync(posDate);
        return Ok(summary);
    }
}
