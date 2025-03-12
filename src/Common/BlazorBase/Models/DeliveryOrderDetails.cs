namespace BlazorBase.Models;

public class DineInOrderDetails
{
    public string OrderType { get; } = "DineIn";
    public OrderDetails? BasicOrderDetails { get; set; } = new();

    public int RelatedTableId { get; set; }
    public string? RelatedTableName { get; set; }
    public string? CaptainId { get; set; }
    public string? CaptainName { get; set; }
}