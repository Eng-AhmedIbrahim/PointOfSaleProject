
namespace POS.Reports.Models;

public record Receipt
{
    public int Id { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public string ReceiptType { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string FooterMessage { get; set; } = string.Empty;
    public string LogoPath { get; set; } = string.Empty;
    private List<ReceiptItem> _items = [];
    public List<TableItem>? Items { get; set; }
    public decimal? SubTotal { get; set; } 
    public decimal? Discount { get; set; }  
    public decimal? TotalAmount { get; set; } 
    public decimal? Services { get; set; } 
    public decimal? Tax { get; set; }
    public DateTimeOffset DateCreated { get; set; }
    public float LogoWidth { get; set; } = 120f;
    public bool IsCopy { get; set; }
    public int? TableId { get; set; }
    public string? TableName { get; set; }
    public string? WaiterName { get; set; }

    public void AddItem(ReceiptItem item) => _items.Add(item);
    public void AddItems(IEnumerable<ReceiptItem> items) => _items.AddRange(items);
}