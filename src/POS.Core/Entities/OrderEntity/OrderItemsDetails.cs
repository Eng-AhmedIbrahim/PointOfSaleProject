using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using POS.Core.Entities.DineIn;

namespace POS.Core.Entities.OrderEntity;

public class OrderItemsDetails:BaseEntity
{
    public int? OrderId { get; set; }
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
    
    // Denormalized item details
    public string? ItemName { get; set; }
    public string? ItemNameAr { get; set; }
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public decimal? UnitPrice { get; set; }
    
    [NotMapped]
    public int? ItemKitchenTypeId { get; set; }
    [NotMapped]
    public int? CategoryKitchenTypeId { get; set; }
    [NotMapped]
    public bool? PrintInBackupReceiptFromCategory { get; set; }
    [NotMapped]
    public bool? PrintInBackupReceiptFromItem { get; set; }

    public Orders? Order { get; set; }
    
    public int? DineInOrderId { get; set; }
    public DineInOrder? DineInOrder { get; set; }

    public ICollection<OrderItemAttributes>? OrderItemAttributes { get; set; } = new List<OrderItemAttributes>();
    public ICollection<OrderItemComment>? OrderItemComments { get; set; } = new List<OrderItemComment>();
}