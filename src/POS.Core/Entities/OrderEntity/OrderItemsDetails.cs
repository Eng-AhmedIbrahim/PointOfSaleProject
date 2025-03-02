namespace POS.Core.Entities.OrderEntity;

public class OrderItemsDetails:BaseEntity
{
    public int OrderId { get; set; }
    public OrderTypes OrderType { get; set; } 

    public int? MenuSalesItemId { get; set; }
    public MenuSalesItems? MenuSalesItem { get; set; }
    public int? Quantity { get; set; }
    public bool? Discount { get; set; }
    public decimal? TotalDiscountPrice { get; set; }
    public decimal? TotalDiscountPercentage { get; set; }
    public decimal? TotalDiscountAmount { get; set; }
    public decimal? TotalAfterDiscount { get; set; }
    public bool? IsVoided { get; set; }
    public int? VoidAmount { get; set; }
    public decimal? TotalAmount { get; set; }

    public Orders? Order { get; set; }

    public ICollection<OrderItemAttributes>? OrderItemAttributes { get; set; } = new List<OrderItemAttributes>();
}