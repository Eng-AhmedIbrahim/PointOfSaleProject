namespace POS.Reports.Models;

public record ReceiptItem
{
    public string Name { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public decimal Price { get; set; }
    public decimal Total => (decimal)Quantity * Price;
}