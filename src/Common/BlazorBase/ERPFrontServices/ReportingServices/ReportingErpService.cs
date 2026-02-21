using POS.Contract.Dtos.ReportingDtos;
using POS.Contract.Dtos.DineIn;
using System.Net.Http.Json;

namespace BlazorBase.ERPFrontServices.ReportingServices;

public class ReportingErpService : IReportingErpService
{
    private readonly HttpClient _httpClient;

    public ReportingErpService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<SalesSummaryDto> GetSalesSummary(DateTime posDate)
    {
        var response = await _httpClient.GetFromJsonAsync<SalesSummaryDto>($"api/reporting/sales-summary?posDate={posDate:yyyy-MM-dd}");
        return response ?? new SalesSummaryDto();
    }

    public async Task<List<AccountSummaryDto>> GetAccountsSummary(DateTime posDate, string staffType)
    {
        var response = await _httpClient.GetFromJsonAsync<List<AccountSummaryDto>>($"api/reporting/accounts-summary?posDate={posDate:yyyy-MM-dd}&staffType={staffType}");
        return response ?? new List<AccountSummaryDto>();
    }

    public async Task<List<OrderDto>> GetTodayOrders(DateTime posDate, string? orderType = null)
    {
        var url = $"api/reporting/today-orders?posDate={posDate:yyyy-MM-dd}";
        if (!string.IsNullOrEmpty(orderType)) url += $"&orderType={orderType}";
        
        var response = await _httpClient.GetFromJsonAsync<List<OrderDto>>(url);
        return response ?? new List<OrderDto>();
    }

    public async Task<List<SalesItemSummaryDto>> GetSalesItemsSummary(DateTime posDate)
    {
        var response = await _httpClient.GetFromJsonAsync<List<SalesItemSummaryDto>>($"api/reporting/sales-items-summary?posDate={posDate:yyyy-MM-dd}");
        return response ?? new List<SalesItemSummaryDto>();
    }
}
