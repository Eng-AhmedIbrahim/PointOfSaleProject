namespace BlazorBase.Models;

public class OrderDiscount
{
    public decimal Percentage { get; set; }
    public decimal Value { get; set; }
    public string? DiscountReason { get; set; }
}
