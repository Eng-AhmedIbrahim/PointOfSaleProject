namespace POS.Reports.Models.Kitchen;

public class KitchenReceipt
{
    public int Id { get; set; }
    public string KitchenNote { get; set; } = string.Empty;
    public string KitchenType { get; set; } = string.Empty;
    public string OrderType { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public List<TableItem>? Items { get; set; }
    public int? TableId { get; set; }
    public string? TableName { get; set; }
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.Now;

    private List<ReceiptItem> _items = [];
    public void AddItem(ReceiptItem item) => _items.Add(item);
    public void AddItems(IEnumerable<ReceiptItem> items) => _items.AddRange(items);
}