namespace BlazorBase.Models;

public class OrderDetails
{
    public int OrderId { get; set; }
    public DateTime? OrderDataTime { get; set; }
    public string? CashierName { get; set; }
    public string? OrderType { get; set; }
    public List<TableItem> Items { get; set; } = new List<TableItem>();

    public decimal? Account { get; set; }
    public OrderDiscount OrderDiscount { get; set; } = new();

    public decimal? Tax { get; set; }
    public decimal? Service { get; set; }
    public decimal? Total { get; set; }
    public string? OrderNote { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public OrderDetails Clone()
    {
        return new OrderDetails
        {
            OrderId = this.OrderId,
            OrderDataTime = this.OrderDataTime,
            CashierName = this.CashierName,
            OrderType = this.OrderType,
            Account = this.Account,
            OrderDiscount = this.OrderDiscount,
            Tax = this.Tax,
            Service = this.Service,
            Total = this.Total,
            OrderNote = this.OrderNote,
            CustomerName = this.CustomerName,
            CustomerPhone = this.CustomerPhone,
            PaymentMethod = this.PaymentMethod,
            Items = this.Items.Select(item => item.Clone()).ToList() // Deep copy items
        };
    }

    public void Merge(OrderDetails other)
    {
        if (other == null) return;

        Items.AddRange(other.Items);

        Account = (Account ?? 0) + (other.Account ?? 0);
        Tax = (Tax ?? 0) + (other.Tax ?? 0);
        Service = (Service ?? 0) + (other.Service ?? 0);
        Total = (Total ?? 0) + (other.Total ?? 0);

    }

}
