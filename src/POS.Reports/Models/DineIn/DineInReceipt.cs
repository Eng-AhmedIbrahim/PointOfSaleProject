namespace POS.Reports.Models.DineIn;

public class DineInReceipt
{
    public int Id { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public string CashierName { get; set; } = string.Empty;
    public string CaptainName { get; set; } = string.Empty;
    public string ReceiptType { get; set; } = string.Empty;
    public string ReceiptNote { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string FooterMessage { get; set; } = string.Empty;
    public string LogoPath { get; set; } = "F:\\Point Of Sale\\PointOfSale\\src\\ERPFront\\wwwroot\\images\\Logo.png";
    private List<ReceiptItem> _items = [];
    public IReadOnlyList<ReceiptItem> Items => _items;
    public decimal? TotalAmount { get; set; } = 100;
    public string ServiceAmount { get; set; } = string.Empty;
    public string TotalOrder { get; set; } = string.Empty;
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.Now;
    public float LogoWidth { get; set; } = 120f;

    public void AddItem(ReceiptItem item) => _items.Add(item);
    public void AddItems(IEnumerable<ReceiptItem> items) => _items.AddRange(items);
}