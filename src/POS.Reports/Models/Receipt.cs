
namespace POS.Reports.Models;

public record Receipt
{
    public int Id { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public string ReceiptType { get; set; } = string.Empty;
    public string ReceiptNote { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string FooterMessage { get; set; } = string.Empty;
    private List<ReceiptItem> _items = [];
    public IReadOnlyList<ReceiptItem> Items => _items;
    public decimal? TotalAmount { get; set; }
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.Now;
    public void AddItem(ReceiptItem item) => _items.Add(item);
    public void AddItems(IEnumerable<ReceiptItem> items) => _items.AddRange(items);
}