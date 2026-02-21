using POS.Contract.Dtos.ReportingDtos;
using POS.Contract.Dtos.DineIn;

namespace BlazorBase.ERPFrontServices.ReportingServices;

public interface IReportingErpService
{
    Task<SalesSummaryDto> GetSalesSummary(DateTime posDate);
    Task<List<AccountSummaryDto>> GetAccountsSummary(DateTime posDate, string staffType);
    Task<List<OrderDto>> GetTodayOrders(DateTime posDate, string? orderType = null);
    Task<List<SalesItemSummaryDto>> GetSalesItemsSummary(DateTime posDate);
}
