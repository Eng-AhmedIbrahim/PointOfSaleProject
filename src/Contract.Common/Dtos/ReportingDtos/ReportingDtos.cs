using POS.Contract.Dtos.OrderDtos;
using System.Collections.Generic;

namespace POS.Contract.Dtos.ReportingDtos;

public class SalesSummaryDto
{
    public DateTime PosDate { get; set; }
    public string? StaffName { get; set; }
    public List<POS.Contract.Dtos.OrderDtos.OrderDto>? DetailedOrders { get; set; }
    public ModeSummaryDto DineIn { get; set; } = new();
    public ModeSummaryDto Delivery { get; set; } = new();
    public ModeSummaryDto TakeAway { get; set; } = new();
    public OverallSummaryDto Overall { get; set; } = new();
}

public class ModeSummaryDto
{
    public decimal Subtotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }
    public decimal Service { get; set; }
    public decimal DeliveryFees { get; set; }
    public decimal Total { get; set; }
    public decimal UncollectedAmount { get; set; } // Pending/In-progress orders
    public int OrderCount { get; set; }
    public decimal AverageOrder => OrderCount > 0 ? (Total + UncollectedAmount) / OrderCount : 0;
    public decimal PercentageOfSales { get; set; }
}

public class OverallSummaryDto
{
    public decimal TotalSales { get; set; }
    public decimal CashAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal OnAccountAmount { get; set; }
    public decimal PendingAmount { get; set; } // Non-completed orders
    public decimal RefundAmount { get; set; }
    public decimal Expenses { get; set; }
    public decimal VoidAmount { get; set; }
    public decimal VoidCount { get; set; }
    public decimal TotalDiscount { get; set; }
    public decimal TotalRevenue => TotalSales + PendingAmount;
    public decimal NetCash => CashAmount - RefundAmount - Expenses;
    public string Currency { get; set; } = string.Empty;
}

public class AccountSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Cashier, Waiter, Driver
    public decimal CashAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal OnAccountAmount { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal Expenses { get; set; }
    public int OrderCount { get; set; }
    public decimal TotalAmount => CashAmount + CreditAmount + OnAccountAmount;
}

public class SalesItemSummaryDto
{
    public int ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal UnitPrice { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}
