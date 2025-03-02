using POS.Contract.Models;

namespace POS.API.ReportEntities;

public class TakeAwayReceiptDetails
{
    public string? BranchName { get; set; }
    public int OrderNumber { get; set; }
    public string? OrderType { get; set; }
    public string? OrderDate { get; set; }
    public string? OrderTime { get; set; }
    public string? CashierName { get; set; }
    public List<TableItem>? OrderItems { get; set; }
    public string? TotalOrderAmount { get; set; }
    public string? PaymentType { get; set; }
}