namespace POS.Core.Entities.Kitchen;

public class KitchenType :BaseEntity
{
    public int BranchId { get; set; }
    public string? KitchenName { get; set; }
}