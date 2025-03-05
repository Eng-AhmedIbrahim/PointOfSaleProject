namespace POS.Core.Entities.Kitchen;

public class OrderSetting : BaseEntity
{
    public int BranchID { get; set; }
    public string OrderType { get; set; } = string.Empty;
    public string? OrderStatment { get; set; }
    public decimal? Service { get; set; }
    public decimal? Tax { get; set; }
    public decimal? Tips { get; set; }  
    public int? JobID { get; set; }
    public int? CustomerReceiptCount { get; set; }
    public int? FullKitchenReceiptCount { get; set; }
    public int? SeparateReceiptCount { get; set; }
    public int? ClosingReceiptCount { get; set; }
    public bool? AddServiceToItemPrice { get; set; }
}