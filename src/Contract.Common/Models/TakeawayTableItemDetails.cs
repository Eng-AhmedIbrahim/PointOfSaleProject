namespace POS.Contract.Models;

public class TakeawayTableItemDetails
{
    public TableItem? TableItem { get; set; }
    public bool HasDiscount { get; set; }
    public decimal? DiscountPercentage { get; set; } = null;
    public decimal? DiscountAmount { get; set; } = null;
    public bool HasTax { get; set; } = false;
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
}