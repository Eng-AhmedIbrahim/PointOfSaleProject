namespace POS.Core.Entities.Kitchen;

public class KitchenType : BaseEntity
{
    public int BranchId { get; set; }
    public string? KitchenName { get; set; }

    public int? KitchenPrinterId { get; set; }
    public KitchenPrinters? KitchenPrinters { get; set; }

    public ICollection<Category>? Categories { get; set; }
    public ICollection<MenuSalesItems>? Items { get; set; }
}
