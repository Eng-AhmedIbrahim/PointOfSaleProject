namespace POS.Core.Entities.ReportEntities;

public class TakeAwayReceiptDetails
{
    public string? BranchName { get; set; }
    public int OrderNumber { get; set; }
    public string? OrderType { get; set; }
    public string? OrderDate { get; set; }
    public string? OrderTime { get; set; }
    public string? CashierName { get; set; }
    public ICollection<OrderItems>? OrderItems { get; set; }
    public string? TotalOrderAmount { get; set; }
    public string? PaymentType { get; set; }
}


public class OrderItems
{
    public string? Quantity { get; set; }
    public string? ItemName { get; set; }
    public string? Price { get; set; }
    public string? ItemTotal { get; set; }
}