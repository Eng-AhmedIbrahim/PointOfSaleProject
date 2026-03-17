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

    public async Task<SalesSummaryDto> GetSalesSummaryAsync(DateTime posDate, DateTime? endDate = null)
    {
        var finalEndDate = endDate ?? posDate;
        
        var spec = new BaseSpecifications<Orders>(o => 
            (o.OrderDate.HasValue && o.OrderDate.Value.Date >= posDate.Date.Date && o.OrderDate.Value.Date <= finalEndDate.Date.Date) ||
            (o.VoidTime.HasValue && o.VoidTime.Value.Date >= posDate.Date.Date && o.VoidTime.Value.Date <= finalEndDate.Date.Date));
        
        
        spec.Includes.Add(o => o.OrderDetails!);
        spec.AddThenInclude("OrderDetails.MenuSalesItem.Category");
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);

        var completedOrders = orders.Where(o => o.OrderState == OrderStates.Completed).ToList();
        var pendingOrders = orders.Where(o => o.OrderState != OrderStates.Completed 
        && o.OrderState != OrderStates.Voided && o.OrderState != OrderStates.Canceled).ToList();

        var summary = new SalesSummaryDto { PosDate = posDate };

        summary.DineIn = MapToModeSummary(orders, OrderTypes.DineIn);
        summary.Delivery = MapToModeSummary(orders, OrderTypes.Delivery);
        summary.TakeAway = MapToModeSummary(orders, OrderTypes.TakeAway);

        summary.Overall.TotalSales = completedOrders.Sum(o => o.GrandTotal ?? 0);
        
        summary.Overall.CashAmount = completedOrders.Where(o => o.PaymentMethod == PaymentMethod.Cash)
            .Sum(o => (o.GrandTotal ?? 0));
        
        summary.Overall.CreditAmount = completedOrders.Where(o => o.PaymentMethod != PaymentMethod.Cash)
            .Sum(o => (o.GrandTotal ?? 0));
        
        summary.Overall.PendingAmount = pendingOrders.Sum(o => o.GrandTotal ?? 0);
        
        // Use OrderVoid table for accurate void accounting (includes voids of old orders)
        var voidSpec = new BaseSpecifications<OrderVoid>(v => v.VoidDate.Date >= posDate.Date && v.VoidDate.Date <= finalEndDate.Date);
        var voidsActivity = await _unitOfWork.Repository<OrderVoid>().GetAllWithSpecificationAsync(voidSpec);
        
        summary.Overall.FullVoidAmount = voidsActivity.Where(v => v.IsFullVoid).Sum(v => v.TotalVoidedAmount);
        summary.Overall.PartialVoidAmount = voidsActivity.Where(v => !v.IsFullVoid).Sum(v => v.TotalVoidedAmount);
        summary.Overall.VoidAmount = summary.Overall.FullVoidAmount + summary.Overall.PartialVoidAmount;
        summary.Overall.VoidCount = voidsActivity.Count;

        summary.VoidEvents = voidsActivity.Select(v => new VoidEventDto
        {
            OrderId = v.OrderId,
            OrderType = v.OrderType.ToString(),
            VoidDate = v.VoidDate,
            VoidedByName = v.VoidedByName,
            Reason = v.Reason,
            IsFullVoid = v.IsFullVoid,
            GrandTotalBefore = v.GrandTotalBefore,
            TotalVoidedAmount = v.TotalVoidedAmount,
            GrandTotalAfter = v.GrandTotalAfter
        }).OrderByDescending(v => v.VoidDate).ToList();
        
        summary.Overall.TotalDiscount = summary.DineIn.Discount + summary.Delivery.Discount + summary.TakeAway.Discount;
        
        // Population of DetailedOrders for report sub-tables
        summary.DetailedOrders = _mapper.Map<List<OrderDto>>(orders.OrderByDescending(o => o.OrderDate).ToList());

        // Analytics: Hourly Sales
        summary.Overall.HourlySales = completedOrders
            .Where(o => o.OrderDate.HasValue)
            .GroupBy(o => o.OrderDate.Value.Hour)
            .Select(g => new HourlySalesDto 
            { 
                Hour = g.Key, 
                HourLabel = (g.Key == 0) ? "12 صباحاً" : (g.Key == 12) ? "12 مساءً" : (g.Key > 12) ? $"{g.Key - 12} مساءً" : $"{g.Key} صباحاً", 
                Amount = g.Sum(o => o.GrandTotal ?? 0), 
                OrderCount = g.Count(),
                DineInAmount = g.Where(o => o.OrderType == OrderTypes.DineIn).Sum(o => o.GrandTotal ?? 0),
                DineInCount = g.Count(o => o.OrderType == OrderTypes.DineIn),
                TakeAwayAmount = g.Where(o => o.OrderType == OrderTypes.TakeAway).Sum(o => o.GrandTotal ?? 0),
                TakeAwayCount = g.Count(o => o.OrderType == OrderTypes.TakeAway),
                DeliveryAmount = g.Where(o => o.OrderType == OrderTypes.Delivery).Sum(o => o.GrandTotal ?? 0),
                DeliveryCount = g.Count(o => o.OrderType == OrderTypes.Delivery)
            })
            .OrderBy(h => h.Hour)
            .ToList();

        // Analytics: Mode Details (Rich Breakdown)
        summary.Overall.ModeDetails = new List<ModeDetails>
        {
            CreateModeDetail("الصالة", orders, OrderTypes.DineIn),
            CreateModeDetail("التوصيل", orders, OrderTypes.Delivery),
            CreateModeDetail("التيك أواي", orders, OrderTypes.TakeAway)
        };

        decimal totalValue = summary.Overall.TotalSales + summary.Overall.PendingAmount;
        if (totalValue > 0)
        {
            summary.DineIn.PercentageOfSales = (summary.DineIn.Total + summary.DineIn.UncollectedAmount) / totalValue * 100;
            summary.Delivery.PercentageOfSales = (summary.Delivery.Total + summary.Delivery.UncollectedAmount) / totalValue * 100;
            summary.TakeAway.PercentageOfSales = (summary.TakeAway.Total + summary.TakeAway.UncollectedAmount) / totalValue * 100;
        }

        summary.Overall.Currency = "L.E";

        // Cashier Summaries
        summary.CashierSummaries = completedOrders
            .Where(o => !string.IsNullOrEmpty(o.CashierID))
            .GroupBy(o => o.CashierID!)
            .Select(g => new AccountSummaryDto
            {
                Id = g.Key,
                Name = g.First().CashierName ?? "Unknown",
                Type = "Cashier",
                CashAmount = g.Where(o => o.PaymentMethod == PaymentMethod.Cash).Sum(o => o.GrandTotal ?? 0),
                CreditAmount = g.Where(o => o.PaymentMethod == PaymentMethod.Visa).Sum(o => o.GrandTotal ?? 0),
                OrderCount = g.Count()
            }).ToList();

        return summary;
    }

    private ModeDetails CreateModeDetail(string title, IEnumerable<Orders> orders, OrderTypes type)
    {
        var modeOrders = orders.Where(o => o.OrderType == type).ToList();
        var completed = modeOrders.Where(o => o.OrderState == OrderStates.Completed).ToList();
        
        return new ModeDetails
        {
            ModeTitle = title,
            Subtotal = completed.Sum(o => o.Subtotal ?? 0),
            Discount = completed.Sum(o => o.Discount ?? 0),
            TotalTaxAndService = completed.Sum(o => (o.Tax ?? 0) + (o.Service ?? 0) + (o.DeliveryFees ?? 0)),
            GrandTotal = completed.Sum(o => o.GrandTotal ?? 0),
            OrderCount = modeOrders.Count
        };
    }

    private ModeSummaryDto MapToModeSummary(IEnumerable<Orders> orders, OrderTypes type)
    {
        var modeOrders = orders.Where(o => o.OrderType == type).ToList();
        var completed = modeOrders.Where(o => o.OrderState == OrderStates.Completed).ToList();
        var pending = modeOrders.Where(o => o.OrderState != OrderStates.Completed 
            && o.OrderState != OrderStates.Voided 
            && o.OrderState != OrderStates.Canceled).ToList();

        return new ModeSummaryDto
        {
            Subtotal = completed.Sum(o => (o.Subtotal ?? 0) - (o.Discount ?? 0)),
            Discount = completed.Sum(o => o.Discount ?? 0),
            Tax = completed.Sum(o => o.Tax ?? 0),
            Service =  completed.Sum(o => o.Service ?? 0),
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

        if (staffType.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            var cashiers = orders.Where(o => !string.IsNullOrEmpty(o.CashierID))
                .GroupBy(o => o.CashierID!)
                .Select(g => new AccountSummaryDto { Id = g.Key, Name = g.First().CashierName ?? "Unknown", Type = "Cashier", OrderCount = g.Count() });

            var waiters = orders.Where(o => !string.IsNullOrEmpty(o.WaiterID))
                .GroupBy(o => o.WaiterID!)
                .Select(g => new AccountSummaryDto { Id = g.Key, Name = g.First().WaiterName ?? "Unknown", Type = "Waiter", OrderCount = g.Count() });

            var drivers = orders.Where(o => !string.IsNullOrEmpty(o.DriverID))
                .GroupBy(o => o.DriverID!)
                .Select(g => new AccountSummaryDto { Id = g.Key, Name = g.First().DriverName ?? "Unknown", Type = "Driver", OrderCount = g.Count() });

            return cashiers.Concat(waiters).Concat(drivers)
                .GroupBy(s => new { s.Id, s.Type })
                .Select(g => g.First())
                .OrderBy(s => s.Name)
                .ToList();
        }

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
        // Include orders opened today OR orders voided today
        Expression<Func<Orders, bool>> criteria = o => 
            (o.OrderDate.HasValue && o.OrderDate.Value.Date == posDate.Date) || 
            (o.VoidTime.HasValue && o.VoidTime.Value.Date == posDate.Date);

        if (!string.IsNullOrEmpty(orderType) && Enum.TryParse<OrderTypes>(orderType, out var type))
        {
            var original = criteria;
            criteria = o => ((o.OrderDate.HasValue && o.OrderDate.Value.Date == posDate.Date) || 
                             (o.VoidTime.HasValue && o.VoidTime.Value.Date == posDate.Date)) 
                            && o.OrderType == type;
        }

        var spec = new BaseSpecifications<Orders>(criteria);
        spec.Includes.Add(o => o.OrderDetails!);
        spec.AddThenInclude("OrderDetails.OrderItemAttributes");
        spec.AddThenInclude("OrderDetails.MenuSalesItem.Category");
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
        return _mapper.Map<List<OrderDto>>(orders.OrderByDescending(o => o.OrderDate).ToList());
    }

    public async Task<List<OrderDto>> GetStaffOrdersAsync(DateTime posDate, string staffId, string staffType)
    {
        Expression<Func<Orders, bool>> criteria = o => (o.OrderDate.HasValue && o.OrderDate.Value.Date == posDate.Date) || (o.VoidTime.HasValue && o.VoidTime.Value.Date == posDate.Date);

        bool isAll = staffId.Equals("All", StringComparison.OrdinalIgnoreCase);

        if (staffType.Equals("Cashier", StringComparison.OrdinalIgnoreCase))
            criteria = o => ((o.OrderDate.HasValue && o.OrderDate.Value.Date == posDate.Date) || (o.VoidTime.HasValue && o.VoidTime.Value.Date == posDate.Date)) && (isAll ? o.CashierID != null : o.CashierID == staffId);
        else if (staffType.Equals("Waiter", StringComparison.OrdinalIgnoreCase))
            criteria = o => ((o.OrderDate.HasValue && o.OrderDate.Value.Date == posDate.Date) || (o.VoidTime.HasValue && o.VoidTime.Value.Date == posDate.Date)) && (isAll ? o.WaiterID != null : o.WaiterID == staffId);
        else if (staffType.Equals("Driver", StringComparison.OrdinalIgnoreCase))
            criteria = o => ((o.OrderDate.HasValue && o.OrderDate.Value.Date == posDate.Date) || (o.VoidTime.HasValue && o.VoidTime.Value.Date == posDate.Date)) && (isAll ? o.DriverID != null : o.DriverID == staffId);

        var spec = new BaseSpecifications<Orders>(criteria);
        spec.Includes.Add(o => o.OrderDetails!);
        spec.AddThenInclude("OrderDetails.OrderItemAttributes");
        spec.AddThenInclude("OrderDetails.MenuSalesItem.Category");
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
        return _mapper.Map<List<OrderDto>>(orders.OrderByDescending(o => o.OrderDate).ToList());
    }

    public async Task<List<SalesItemSummaryDto>> GetSalesItemsSummaryAsync(DateTime posDate, DateTime? endDate = null)
    {
        var finalEndDate = endDate ?? posDate;
        // Only include COMPLETED orders to match the financial summary totals
        var spec = new BaseSpecifications<Orders>(
            o => o.OrderDate.HasValue && o.OrderDate.Value.Date >= posDate.Date && o.OrderDate.Value.Date <= finalEndDate.Date && 
                 o.OrderState == OrderStates.Completed);

        spec.Includes.Add(o => o.OrderDetails!);
        spec.AddThenInclude("OrderDetails.OrderItemAttributes");
        spec.AddThenInclude("OrderDetails.MenuSalesItem.Category");
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);

        var menuItems = await _unitOfWork.Repository<MenuSalesItems>().GetAllAsync();
        var categories = await _unitOfWork.Repository<Category>().GetAllAsync();
        var attributeItems = await _unitOfWork.Repository<AttributeItem>().GetAllAsync();
        var menuDict = menuItems.ToDictionary(m => m.Id);
        var catDict = categories.ToDictionary(c => c.Id);
        var attrItemToMenuItemDict = attributeItems.ToDictionary(a => a.Id, a => a.RelatedMenuItemId);

        var rawRows = new List<(int MenuSalesItemId, string ItemName, string CategoryName, decimal Quantity, decimal TotalAmount, decimal UnitPrice, string Unit)>();

        foreach (var order in orders)
        {
            if (order.OrderDetails == null) continue;
            foreach (var d in order.OrderDetails)
            {
                if (d.IsVoided == true) continue;

                var itemName = d.ItemName ?? "";
                var catName = d.CategoryName ?? "";
                var unit = "قطعة";
                MenuSalesItems? menuItem = null;
                if (d.MenuSalesItemId.HasValue && menuDict.TryGetValue(d.MenuSalesItemId.Value, out menuItem))
                {
                    if (string.IsNullOrWhiteSpace(itemName)) itemName = menuItem.ArabicName ?? menuItem.EnglishName ?? "";
                    if (string.IsNullOrWhiteSpace(catName) && menuItem.CategoryId.HasValue && catDict.TryGetValue(menuItem.CategoryId.Value, out var cat))
                        catName = cat.ArabicName ?? cat.EnglishName ?? "";
                    
                    if (menuItem.ByWeight) unit = "كجم";
                }

                var lineTotal = d.TotalAmount ?? 0;

                var rawExtraPriceTotal = d.OrderItemAttributes?.Sum(a => (a.ExtraPrice ?? 0) * (d.Quantity ?? 0)) ?? 0;
                var mainItemTotal = lineTotal - rawExtraPriceTotal;

                rawRows.Add((d.MenuSalesItemId ?? 0, itemName, catName, d.Quantity ?? 0, mainItemTotal, menuItem?.Price ?? 0, unit));

                if (d.OrderItemAttributes != null && d.OrderItemAttributes.Any())
                {
                    var itemQty = d.Quantity ?? 0;
                    foreach (var attr in d.OrderItemAttributes)
                    {
                        if (attr.AttributeItemId.HasValue && attrItemToMenuItemDict.TryGetValue(attr.AttributeItemId.Value, out var relatedMenuItemId))
                        {
                            var relatedItemName = "";
                            var relatedCatName = "";
                            var relatedUnit = "قطعة";
                            if (menuDict.TryGetValue(relatedMenuItemId, out var relatedItem))
                            {
                                relatedItemName = relatedItem.ArabicName ?? relatedItem.EnglishName ?? "";
                                if (relatedItem.CategoryId.HasValue && catDict.TryGetValue(relatedItem.CategoryId.Value, out var relatedCat))
                                    relatedCatName = relatedCat.ArabicName ?? relatedCat.EnglishName ?? "";
                                
                                if (relatedItem.ByWeight) relatedUnit = "كجم";
                            }
                            
                            var attrEffectiveTotal = (attr.ExtraPrice ?? 0) * itemQty;
                            rawRows.Add((relatedMenuItemId, relatedItemName, relatedCatName, itemQty, attrEffectiveTotal, relatedItem?.Price ?? 0, relatedUnit));
                        }
                    }
                }
            }
        }

        return rawRows
            .GroupBy(r => new { r.MenuSalesItemId, r.ItemName, r.CategoryName, r.Unit })
            .Select(g => new SalesItemSummaryDto
            {
                ItemId = g.Key.MenuSalesItemId,
                ItemName = string.IsNullOrWhiteSpace(g.Key.ItemName) ? "صنف غير معروف" : g.Key.ItemName,
                CategoryName = string.IsNullOrWhiteSpace(g.Key.CategoryName) ? "بدون تصنيف" : g.Key.CategoryName,
                Quantity = g.Sum(r => r.Quantity),
                TotalAmount = g.Sum(r => r.TotalAmount),
                UnitPrice = g.Sum(r => r.Quantity) > 0 ? (g.Sum(r => r.TotalAmount) / g.Sum(r => r.Quantity)) : 0,
                Unit = g.Key.Unit
            })
            .OrderByDescending(i => i.TotalAmount)
            .ToList();
    }
    public async Task<List<OrderDto>> GetPendingOrdersAsync(DateTime posDate)
    {
        Expression<Func<Orders, bool>> criteria = o => 
            o.OrderDate.HasValue && 
            o.OrderDate.Value.Date == posDate.Date &&
            o.OrderState != OrderStates.Completed &&
            o.OrderState != OrderStates.Voided &&
            o.OrderState != OrderStates.Canceled;

        var spec = new BaseSpecifications<Orders>(criteria);
        spec.Includes.Add(o => o.OrderDetails!);
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
        return _mapper.Map<List<OrderDto>>(orders.ToList());
    }
}
