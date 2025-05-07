namespace BlazorBase.Models;

public class DineInOrderDetails
{
    public string OrderType { get; } = "DineIn";
    public OrderDetails? BasicOrderDetails { get; set; } = new();

    public int? RelatedTableId { get; set; }
    public string? RelatedTableName { get; set; }
    public string? CaptainId { get; set; }
    public string? CaptainName { get; set; }

    public DineInOrderDetails Clone()
    {
        return new DineInOrderDetails
        {
            RelatedTableId = this.RelatedTableId,
            RelatedTableName = this.RelatedTableName,
            CaptainId = this.CaptainId,
            CaptainName = this.CaptainName,
            BasicOrderDetails = this.BasicOrderDetails?.Clone()
        };
    }
    public bool IsMigrated { get; set; } = false;
    public List<int> MergedTableIds { get; set; } = new();


}
