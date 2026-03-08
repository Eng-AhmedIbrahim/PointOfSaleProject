using POS.Contract.Dtos.ReportingDtos;
using POS.Contract.Dtos.DineIn;

namespace POS.Core.Services.Contract.ReportingServices;

public interface IReportingService
{
    Task<SalesSummaryDto> GetSalesSummaryAsync(DateTime posDate);
    Task<List<AccountSummaryDto>> GetAccountsSummaryAsync(DateTime posDate, string staffType);
    Task<List<OrderDto>> GetTodayOrdersAsync(DateTime posDate, string? orderType = null);
    Task<List<OrderDto>> GetStaffOrdersAsync(DateTime posDate, string staffId, string staffType);
    Task<List<SalesItemSummaryDto>> GetSalesItemsSummaryAsync(DateTime posDate);
}
