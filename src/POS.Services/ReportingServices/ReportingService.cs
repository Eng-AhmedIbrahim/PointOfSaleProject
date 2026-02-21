namespace POS.Services.ReportingServices;

public class ReportingService : IReportingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReportingService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(DateTime posDate)
    {
        var spec = new BaseSpecifications<Orders>(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == posDate.Date);
        spec.Includes.Add(o => o.OrderDetails!);
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);

        var completedOrders = orders.Where(o => o.OrderState == OrderStates.Completed).ToList();
        var pendingOrders = orders.Where(o => o.OrderState != OrderStates.Completed && o.OrderState != OrderStates.Voided && o.OrderState != OrderStates.Canceled).ToList();

        var summary = new SalesSummaryDto { PosDate = posDate };

        summary.DineIn = MapToModeSummary(orders, OrderTypes.DineIn);
        summary.Delivery = MapToModeSummary(orders, OrderTypes.Delivery);
        summary.TakeAway = MapToModeSummary(orders, OrderTypes.TakeAway);

        summary.Overall.TotalSales = completedOrders.Sum(o => o.GrandTotal ?? 0);
        summary.Overall.CashAmount = completedOrders.Where(o => o.PaymentMethod == PaymentMethod.Cash).Sum(o => o.GrandTotal ?? 0);
        summary.Overall.CreditAmount = completedOrders.Where(o => o.PaymentMethod != PaymentMethod.Cash).Sum(o => o.GrandTotal ?? 0);
        summary.Overall.OnAccountAmount = completedOrders.Sum(o => o.Remain ?? 0);
        summary.Overall.PendingAmount = pendingOrders.Sum(o => o.GrandTotal ?? 0);
        
        var voidedOrders = orders.Where(o => o.OrderState == OrderStates.Voided).ToList();
        summary.Overall.VoidAmount = voidedOrders.Sum(o => o.GrandTotal ?? 0);
        summary.Overall.VoidCount = voidedOrders.Count;

        decimal total = summary.Overall.TotalSales + summary.Overall.PendingAmount;
        if (total > 0)
        {
            summary.DineIn.PercentageOfSales = (summary.DineIn.Total + summary.DineIn.UncollectedAmount) / total * 100;
            summary.Delivery.PercentageOfSales = (summary.Delivery.Total + summary.Delivery.UncollectedAmount) / total * 100;
            summary.TakeAway.PercentageOfSales = (summary.TakeAway.Total + summary.TakeAway.UncollectedAmount) / total * 100;
        }

        summary.Overall.Currency = "L.E";
        return summary;
    }

    private ModeSummaryDto MapToModeSummary(IEnumerable<Orders> orders, OrderTypes type)
    {
        var modeOrders = orders.Where(o => o.OrderType == type).ToList();
        var completed = modeOrders.Where(o => o.OrderState == OrderStates.Completed).ToList();
        var pending = modeOrders.Where(o => o.OrderState != OrderStates.Completed && o.OrderState != OrderStates.Voided && o.OrderState != OrderStates.Canceled).ToList();

        return new ModeSummaryDto
        {
            Subtotal = completed.Sum(o => o.Subtotal ?? 0),
            Discount = completed.Sum(o => o.Discount ?? 0),
            Tax = completed.Sum(o => o.Tax ?? 0),
            Service = type == OrderTypes.Delivery 
                ? completed.Sum(o => (o.Service ?? 0) + (o.DeliveryFees ?? 0)) 
                : completed.Sum(o => o.Service ?? 0),
            DeliveryFees = completed.Sum(o => o.DeliveryFees ?? 0),
            Total = completed.Sum(o => o.GrandTotal ?? 0),
            UncollectedAmount = pending.Sum(o => o.GrandTotal ?? 0),
            OrderCount = modeOrders.Count
        };
    }

    public async Task<List<AccountSummaryDto>> GetAccountsSummaryAsync(DateTime posDate, string staffType)
    {
        var spec = new BaseSpecifications<Orders>(o => o.OrderDate.HasValue && o.OrderDate.Value.Date == posDate.Date && o.OrderState == OrderStates.Completed);
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);

        IEnumerable<IGrouping<string, Orders>> groups;

        if (staffType.Equals("Cashier", StringComparison.OrdinalIgnoreCase))
            groups = orders.Where(o => !string.IsNullOrEmpty(o.CashierID)).GroupBy(o => o.CashierID!);
        else if (staffType.Equals("Waiter", StringComparison.OrdinalIgnoreCase))
            groups = orders.Where(o => !string.IsNullOrEmpty(o.WaiterID)).GroupBy(o => o.WaiterID!);
        else // Driver
            groups = orders.Where(o => !string.IsNullOrEmpty(o.DriverID)).GroupBy(o => o.DriverID!);

        return groups.Select(g => new AccountSummaryDto
        {
            Id = g.Key,
            Name = staffType.Equals("Cashier", StringComparison.OrdinalIgnoreCase) ? g.First().CashierName ?? "Unknown" :
                   staffType.Equals("Waiter", StringComparison.OrdinalIgnoreCase) ? g.First().WaiterName ?? "Unknown" :
                   g.First().DriverName ?? "Unknown",
            Type = staffType,
            CashAmount = g.Where(o => o.PaymentMethod == PaymentMethod.Cash).Sum(o => o.GrandTotal ?? 0),
            CreditAmount = g.Where(o => o.PaymentMethod == PaymentMethod.Visa).Sum(o => o.GrandTotal ?? 0),
            OnAccountAmount = g.Sum(o => o.Remain ?? 0),
            OrderCount = g.Count()
        }).ToList();
    }

    public async Task<List<OrderDto>> GetTodayOrdersAsync(DateTime posDate, string? orderType = null)
    {
        Expression<Func<Orders, bool>> criteria = o => o.OrderDate.HasValue && o.OrderDate.Value.Date == posDate.Date;

        if (!string.IsNullOrEmpty(orderType) && Enum.TryParse<OrderTypes>(orderType, out var type))
        {
            criteria = o => o.OrderDate.HasValue && o.OrderDate.Value.Date == posDate.Date && o.OrderType == type;
        }

        var spec = new BaseSpecifications<Orders>(criteria);
        spec.Includes.Add(o => o.OrderDetails!);
        spec.AddThenInclude("OrderDetails.OrderItemAttributes");
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
        return _mapper.Map<List<OrderDto>>(orders.OrderByDescending(o => o.OrderDate).ToList());
    }

    public async Task<List<SalesItemSummaryDto>> GetSalesItemsSummaryAsync(DateTime posDate)
    {
        var spec = new BaseSpecifications<Orders>(
            o => o.OrderDate.HasValue && o.OrderDate.Value.Date == posDate.Date && 
                 o.OrderState != OrderStates.Voided && o.OrderState != OrderStates.Canceled);

        spec.Includes.Add(o => o.OrderDetails!);
        spec.AddThenInclude("OrderDetails.OrderItemAttributes");
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);

        var menuItems = await _unitOfWork.Repository<MenuSalesItems>().GetAllAsync();
        var categories = await _unitOfWork.Repository<Category>().GetAllAsync();
        var attributeItems = await _unitOfWork.Repository<AttributeItem>().GetAllAsync();
        var menuDict = menuItems.ToDictionary(m => m.Id);
        var catDict = categories.ToDictionary(c => c.Id);
        // Map AttributeItemId -> RelatedMenuItemId (which regular item this attribute component refers to)
        var attrItemToMenuItemDict = attributeItems.ToDictionary(a => a.Id, a => a.RelatedMenuItemId);

        // Build flat list of item rows (regular items + expanded attribute sub-items)
        var rawRows = new List<(int MenuSalesItemId, string ItemName, string CategoryName, int Quantity, decimal TotalAmount)>();

        foreach (var order in orders)
        {
            if (order.OrderDetails == null) continue;
            foreach (var d in order.OrderDetails)
            {
                if (d.IsVoided == true) continue;

                var itemName = d.ItemName ?? "";
                var catName = d.CategoryName ?? "";

                if (d.MenuSalesItemId.HasValue && menuDict.TryGetValue(d.MenuSalesItemId.Value, out var menuItem))
                {
                    if (string.IsNullOrWhiteSpace(itemName)) itemName = menuItem.ArabicName ?? menuItem.EnglishName ?? "";
                    if (string.IsNullOrWhiteSpace(catName) && menuItem.CategoryId.HasValue && catDict.TryGetValue(menuItem.CategoryId.Value, out var cat))
                    {
                        catName = cat.ArabicName ?? cat.EnglishName ?? "";
                    }
                }

                // Add the item itself (offer or regular) to the summary
                rawRows.Add((
                    d.MenuSalesItemId ?? 0,
                    itemName,
                    catName,
                    d.Quantity ?? 0,
                    d.TotalAfterDiscount ?? d.TotalAmount ?? 0
                ));

                // If this item has attributes (it's an offer), expand its attribute sub-items
                // and add their quantities to the corresponding regular items
                if (d.OrderItemAttributes != null && d.OrderItemAttributes.Any())
                {
                    var itemQty = d.Quantity ?? 1;
                    foreach (var attr in d.OrderItemAttributes)
                    {
                        if (attr.AttributeItemId.HasValue && attrItemToMenuItemDict.TryGetValue(attr.AttributeItemId.Value, out var relatedMenuItemId))
                        {
                            var relatedItemName = "";
                            var relatedCatName = "";
                            if (menuDict.TryGetValue(relatedMenuItemId, out var relatedItem))
                            {
                                relatedItemName = relatedItem.ArabicName ?? relatedItem.EnglishName ?? "";
                                if (relatedItem.CategoryId.HasValue && catDict.TryGetValue(relatedItem.CategoryId.Value, out var relatedCat))
                                {
                                    relatedCatName = relatedCat.ArabicName ?? relatedCat.EnglishName ?? "";
                                }
                            }
                            // Each attribute sub-item gets quantity = offer quantity (e.g., 1 offer with 2 foul => 1 * 1 = 1 foul)
                            // The attribute represents a single component, so each attr adds 1 * offer qty
                            rawRows.Add((
                                relatedMenuItemId,
                                relatedItemName,
                                relatedCatName,
                                itemQty, // quantity of the offer
                                0 // price is already accounted for in the offer total
                            ));
                        }
                    }
                }
            }
        }

        var items = rawRows
            .GroupBy(r => new { r.MenuSalesItemId, r.ItemName, r.CategoryName })
            .Select(g => new SalesItemSummaryDto
            {
                ItemId = g.Key.MenuSalesItemId,
                ItemName = string.IsNullOrWhiteSpace(g.Key.ItemName) ? "صنف غير معروف" : g.Key.ItemName,
                CategoryName = string.IsNullOrWhiteSpace(g.Key.CategoryName) ? "بدون تصنيف" : g.Key.CategoryName,
                Quantity = g.Sum(r => r.Quantity),
                TotalAmount = g.Sum(r => r.TotalAmount)
            })
            .OrderByDescending(x => x.Quantity)
            .ToList();

        return items;
    }
}
