namespace POS.Core.Entities.Kitchen;

public class KitchenType : BaseEntity
{
    public int BranchId { get; set; }
    public string? KitchenName { get; set; }

    public int? KitchenPrinterId { get; set; }
    public KitchenPrinters? KitchenPrinters { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<Category>? Categories { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public ICollection<MenuSalesItems>? Items { get; set; }
}
