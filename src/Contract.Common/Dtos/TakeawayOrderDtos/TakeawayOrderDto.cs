namespace POS.Contract.Dtos.TakeawayOrderDtos;

public class TakeawayOrderDto
{
    public string? CashierName { get; set; }
    public List<TakeawayTableItemDetails>? OrderDetails { get; set; }
    public int OrderId { get; set; }
    public int BranchId { get; set; }
    public string? BranchName { get; set; }
    public int CashierId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public decimal? Paid { get; set; }
    public decimal? Remaining { get; set; }
    public decimal? TotalOrderAmount { get; set; }
}   