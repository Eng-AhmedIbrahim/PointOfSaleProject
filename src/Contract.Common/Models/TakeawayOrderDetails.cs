namespace POS.Contract.Models;
public class TakeawayOrderDetails
{
    public List<TableItem>? TakeawayItems { get; set; }
    public decimal? Tax { get; set; }
    public decimal? Discount { get; set; }
    public decimal TotalAmount { get; set; }
}