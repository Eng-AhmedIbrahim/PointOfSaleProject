namespace BlazorBase.Models;

public class OrderDetails
{
    public int OrderId { get; set; }
    public DateTime? OrderDataTime { get; set; }
    public string? CashierName { get; set; }
    public string? OrderType { get; set; }
    public List<TableItem> Items { get; set; } = new List<TableItem>();

    public decimal Account { get; set; }
    public decimal? Discount { get; set; }
    public decimal? Tax { get; set; }
    public decimal? Service { get; set; }
    public decimal? Total { get; set; }
    public string? OrderNote { get; set; }
}