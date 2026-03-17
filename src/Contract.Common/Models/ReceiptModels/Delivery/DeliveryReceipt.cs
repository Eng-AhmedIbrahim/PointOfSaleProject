

namespace POS.Contract.Models.ReceiptModels.Delivery;

public class DeliveryReceipt
{
    public string LogoPath { get; set; } = "F:\\Point Of Sale\\PointOfSale\\src\\ERPFront\\wwwroot\\images\\Logo.png";
    public string StoreName { get; set; } = string.Empty;
    public int Id { get; set; }
    public string CashierName { get; set; } = string.Empty;
    public string ReceiptType { get; set; } = string.Empty;
    public string ReceiptNote { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string FooterMessage { get; set; } = string.Empty;
    private List<ReceiptItem> _items = [];
    public IReadOnlyList<ReceiptItem> Items => _items;
    public DateTimeOffset DateCreated { get; set; } = DateTimeOffset.Now;
    public float LogoWidth { get; set; } = 120f;
    public bool IsFollowUp { get; set; }
    public int? ParentOrderId { get; set; }
    public bool IsCopy { get; set; }
    public bool IsVoid { get; set; }


    //Customer Information
    public string? CustomerFirstPhone { get; set; }
    public string? CustomerSecondPhone { get; set; }
    public string? CustomerName { get; set; }
    public string? Building { get; set; }
    public string? HomeNumber { get; set; }
    public string? FloorNumber { get; set; }
    public string? FlatNumber { get; set; }
    public string? ZoneName { get; set; }
    public string? AddressNote { get; set; }
    public string? DeliveryName { get; set; }

    //Amount
    public decimal? TotalAmount { get; set; } = 100;
    public decimal? DeliveryFees { get; set; } = 100;
    public decimal? TotalOrder { get; set; } = 100;
    public string? CustomerAddress { get; set; } = "Tanta";

    public void AddItem(ReceiptItem item) => _items.Add(item);
    public void AddItems(IEnumerable<ReceiptItem> items) => _items.AddRange(items);
}
