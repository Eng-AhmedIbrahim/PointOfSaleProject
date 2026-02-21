using POS.Contract.Dtos.DineIn;
using POS.Contract.Dtos.VoidDtos;
using POS.Core.Entities.DineIn;
using POS.Core.Entities.OrderEntity;
using POS.Core.Repository.Contract;
using POS.Core.Services.Contract.VoidServices;
using POS.Core.Specifications.OrderSpecs;
using POS.Core.Specifications;
using Serilog;

namespace POS.Services.VoidServices;

public class VoidService : IVoidService
{
    private readonly IUnitOfWork _unitOfWork;

    public VoidService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<VoidReportDto>> GetVoidReportAsync(DateTime posDate)
    {
        var dateStart = posDate.Date;
        var dateEnd = posDate.Date.AddDays(1);

        // Get all orders that have any voiding records in history
        var voidSpec = new OrderVoidSpecs(dateStart, dateEnd);

        var voidSessions = await _unitOfWork.Repository<OrderVoid>().GetAllWithSpecificationAsync(voidSpec);
        
        // Group by Order to show summaries or list sessions
        // For compatibility with previous DTO, we might want to return a list of orders with their aggregated void info
        // But the user specifically asked to see WHO and WHEN for MULTIPLE people.
        
        // Let's adapt the existing report to show orders but include nested history
        var orderIds = voidSessions.Select(v => v.OrderId).Distinct().ToList();
        
        var ordersSpec = new BaseSpecifications<Orders>(o => orderIds.Contains(o.Id));
        ordersSpec.Includes.Add(o => o.OrderDetails!);
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(ordersSpec);

        var report = orders.Select(order =>
        {
            var sessions = voidSessions.Where(v => v.OrderId == order.Id).OrderBy(v => v.VoidDate).ToList();
            var lastSession = sessions.LastOrDefault();

            decimal voidedAmt = sessions.Sum(s => s.TotalVoidedAmount);
            decimal currentTotal = order.GrandTotal ?? 0;
            decimal originalTotal = (order.OrderState == OrderStates.Voided && sessions.Any(s => s.IsFullVoid))
                ? voidedAmt 
                : currentTotal + voidedAmt;

            var voidedItems = (order.OrderDetails ?? new List<OrderItemsDetails>())
                .Where(d => (d.VoidAmount ?? 0) > 0 || d.IsVoided == true)
                .Select(d => new VoidItemReportDto
                {
                    OrderDetailId = d.Id,
                    ItemName = d.ItemName,
                    ItemNameAr = d.ItemNameAr,
                    CategoryName = d.CategoryName,
                    UnitPrice = d.UnitPrice,
                    VoidedQuantity = d.VoidAmount ?? 0,
                    RemainingQuantity = d.Quantity ?? 0,
                    VoidedValue = d.TotalVoidAmount,
                    IsFullyVoided = d.IsVoided == true,
                    // Note: These now represent the LATEST void info for this item
                    VoidByName = d.VoidByName,
                    VoidTime = d.VoidTime,
                    VoidReason = d.VoidReason
                }).ToList();

            return new VoidReportDto
            {
                OrderDbId = order.Id,
                OrderId = order.OrderID,
                OrderType = order.OrderType?.ToString(),
                OrderState = order.OrderState?.ToString(),
                OrderDate = order.OrderDate,
                CashierName = order.CashierName,
                CustomerName = order.CustomerName ?? order.TakeawayCustomerName,
                Phone = order.Phone1 ?? order.TakeawayCustomerPhone,
                TableId = order.TableID,
                TableName = order.TableName,
                WaiterName = order.WaiterName,
                DriverName = order.DriverName,
                VoidBy = lastSession?.VoidedBy,
                VoidByName = lastSession?.VoidedByName,
                VoidTime = lastSession?.VoidDate,
                VoidReason = lastSession?.Reason,
                VoidCount = order.VoidCount,
                OriginalTotal = originalTotal,
                VoidedAmount = voidedAmt,
                RemainingTotal = currentTotal,
                IsFullyVoided = order.OrderState == OrderStates.Voided,
                VoidedItems = voidedItems
            };
        }).OrderByDescending(r => r.VoidTime).ToList();

        return report;
    }

    public async Task<bool> VoidOrderAsync(int orderId, string reason, string voidBy, string voidByName)
    {
        using var transaction = _unitOfWork.BeginTransaction();
        try
        {
            var spec = new OrdersByIdSpecs(orderId);
            var order = await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationTrackedAsync(spec);

            if (order is null) return false;

            DateTime voidTime = DateTime.Now;

            // Create Void Header (History)
            var voidOperation = new OrderVoid
            {
                OrderId = order.Id,
                OrderType = order.OrderType ?? OrderTypes.TakeAway,
                OrderStateAtVoid = order.OrderState ?? OrderStates.Pending,
                VoidDate = voidTime,
                VoidedBy = voidBy,
                VoidedByName = voidByName,
                Reason = reason,
                IsFullVoid = true,
                SubtotalBefore = order.Subtotal ?? 0,
                TaxBefore = order.Tax ?? 0,
                ServiceBefore = order.Service ?? 0,
                DeliveryFeesBefore = order.DeliveryFees ?? 0,
                DiscountBefore = order.Discount ?? 0,
                GrandTotalBefore = order.GrandTotal ?? 0
            };

            decimal totalVoidedValue = 0;
            int totalVoidedCount = 0;

            if (order.OrderDetails != null)
            {
                foreach (var item in order.OrderDetails)
                {
                    if (item.IsVoided == true) continue;

                    decimal itemValue = item.TotalAmount ?? 0;
                    int itemQty = item.Quantity ?? 0;

                    var voidItem = new OrderVoidItem
                    {
                        OrderDetailId = item.Id,
                        QuantityBefore = itemQty,
                        QuantityVoided = itemQty,
                        QuantityAfter = 0,
                        AmountBefore = itemValue,
                        AmountVoided = itemValue,
                        AmountAfter = 0,
                        Reason = reason
                    };
                    voidOperation.VoidItems.Add(voidItem);

                    item.IsVoided = true;
                    item.VoidAmount = (item.VoidAmount ?? 0) + itemQty;
                    item.TotalVoidAmount = (item.TotalVoidAmount ?? 0) + itemValue;
                    item.VoidBy = voidBy;
                    item.VoidByName = voidByName;
                    item.VoidTime = voidTime;
                    item.VoidReason = reason;
                    item.Quantity = 0;
                    item.TotalAmount = 0;
                    item.TotalAfterDiscount = 0;

                    totalVoidedValue += itemValue;
                    totalVoidedCount += itemQty;
                }
            }

            order.OrderState = OrderStates.Voided;
            order.VoidTime = voidTime;
            order.VoidByName = voidByName;
            order.VoidBy = voidBy;
            order.VoidReason = reason;
            order.TotalVoid = (order.TotalVoid ?? 0) + totalVoidedValue;
            order.VoidCount = (order.VoidCount ?? 0) + totalVoidedCount;
            order.VoidAmount = (order.VoidAmount ?? 0) + totalVoidedValue;
            order.Subtotal = 0;
            order.GrandTotal = 0;
            order.Tax = 0;
            order.Service = 0;
            order.Paid = 0;
            order.Remain = 0;
            order.ClosingTime = voidTime;

            voidOperation.SubtotalAfter = 0;
            voidOperation.TaxAfter = 0;
            voidOperation.ServiceAfter = 0;
            voidOperation.GrandTotalAfter = 0;
            voidOperation.TotalVoidedAmount = totalVoidedValue;

            await _unitOfWork.Repository<OrderVoid>().AddAsync(voidOperation);

            // Table Release Logic
            if (order.OrderType == OrderTypes.DineIn && order.TableID.HasValue && order.TableID > 0)
            {
                var table = await _unitOfWork.Repository<Table>().GetByIdAsync(order.TableID.Value);
                if (table != null)
                {
                    var openOrdersSpec = new BaseSpecifications<Orders>(o => o.TableID == order.TableID && o.OrderState == OrderStates.Pending && o.Id != orderId);
                    var otherCount = await _unitOfWork.Repository<Orders>().GetCountAsync(openOrdersSpec);
                    if (otherCount == 0)
                    {
                        table.TableState = TableState.Available;
                        _unitOfWork.Repository<Table>().Update(table);
                    }
                }
            }

            await _unitOfWork.CompleteAsync();
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Log.Error(ex, "VoidOrderAsync: Error");
            return false;
        }
    }

    public async Task<bool> VoidItemsAsync(int orderId, List<OrderItemVoidDto> itemsToVoid, string reason, string voidBy, string voidByName)
    {
        if (itemsToVoid == null || !itemsToVoid.Any()) return false;

        using var transaction = _unitOfWork.BeginTransaction();
        try
        {
            var spec = new OrdersByIdSpecs(orderId);
            var order = await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationTrackedAsync(spec);
            if (order is null) return false;

            DateTime voidTime = DateTime.Now;

            var voidOperation = new OrderVoid
            {
                OrderId = order.Id,
                OrderType = order.OrderType ?? OrderTypes.TakeAway,
                OrderStateAtVoid = order.OrderState ?? OrderStates.Pending,
                VoidDate = voidTime,
                VoidedBy = voidBy,
                VoidedByName = voidByName,
                Reason = reason,
                SubtotalBefore = order.Subtotal ?? 0,
                TaxBefore = order.Tax ?? 0,
                ServiceBefore = order.Service ?? 0,
                DeliveryFeesBefore = order.DeliveryFees ?? 0,
                DiscountBefore = order.Discount ?? 0,
                GrandTotalBefore = order.GrandTotal ?? 0
            };

            decimal voidedTotalValue = 0;
            int voidedTotalCount = 0;

            foreach (var voidRequest in itemsToVoid)
            {
                var item = order.OrderDetails?.FirstOrDefault(i => i.Id == voidRequest.OrderItemDetailId || i.MenuSalesItemId == voidRequest.OrderItemDetailId);
                if (item == null || voidRequest.QuantityToVoid <= 0 || item.Quantity < voidRequest.QuantityToVoid) continue;

                int qtyBefore = item.Quantity ?? 0;
                decimal unitPrice = qtyBefore > 0 ? (item.TotalAmount ?? 0) / qtyBefore : (item.UnitPrice ?? 0);
                decimal valToVoid = unitPrice * voidRequest.QuantityToVoid;

                var voidItem = new OrderVoidItem
                {
                    OrderDetailId = item.Id,
                    QuantityBefore = qtyBefore,
                    QuantityVoided = voidRequest.QuantityToVoid,
                    QuantityAfter = qtyBefore - voidRequest.QuantityToVoid,
                    AmountBefore = item.TotalAmount ?? 0,
                    AmountVoided = valToVoid,
                    AmountAfter = (item.TotalAmount ?? 0) - valToVoid,
                    Reason = reason
                };
                voidOperation.VoidItems.Add(voidItem);

                item.VoidAmount = (item.VoidAmount ?? 0) + voidRequest.QuantityToVoid;
                item.TotalVoidAmount = (item.TotalVoidAmount ?? 0) + valToVoid;
                item.VoidBy = voidBy;
                item.VoidByName = voidByName;
                item.VoidTime = voidTime;
                item.VoidReason = reason;
                item.Quantity -= voidRequest.QuantityToVoid;
                item.TotalAmount -= valToVoid;
                if (voidRequest.QuantityToVoid > 0) item.IsVoided = true; // User wants it true even if partially voided 

                voidedTotalValue += valToVoid;
                voidedTotalCount += voidRequest.QuantityToVoid;
            }

            order.TotalVoid = (order.TotalVoid ?? 0) + voidedTotalValue;
            order.VoidCount = (order.VoidCount ?? 0) + voidedTotalCount;
            order.VoidAmount = (order.VoidAmount ?? 0) + voidedTotalValue;

            // Recalculate
            var activeItems = order.OrderDetails?.Where(i => (i.Quantity ?? 0) > 0).ToList() ?? new();
            if (!activeItems.Any())
            {
                order.OrderState = OrderStates.Voided;
                order.Subtotal = 0; order.Tax = 0; order.Service = 0; order.GrandTotal = 0;
                voidOperation.IsFullVoid = true;
            }
            else
            {
                if (order.OrderState != OrderStates.Dispatched && order.OrderState != OrderStates.Voided)
                    order.OrderState = OrderStates.Voided;

                decimal oldGrandTotal = order.GrandTotal ?? 0;
                decimal subBefore = order.Subtotal ?? 1;
                decimal newSub = activeItems.Sum(i => i.TotalAmount ?? 0);
                order.Tax = (order.Tax ?? 0) * (newSub / subBefore);
                order.Service = (order.Service ?? 0) * (newSub / subBefore);
                order.Subtotal = newSub;
                order.GrandTotal = newSub + (order.Tax ?? 0) + (order.Service ?? 0) + (order.DeliveryFees ?? 0) - (order.Discount ?? 0);

                decimal reduction = oldGrandTotal - (order.GrandTotal ?? 0);
                if (reduction > 0)
                {
                    if ((order.Remain ?? 0) >= reduction)
                    {
                        order.Remain -= reduction;
                    }
                    else
                    {
                        decimal diff = reduction - (order.Remain ?? 0);
                        order.Remain = 0;
                        order.Paid = Math.Max(0, (order.Paid ?? 0) - diff);
                    }
                }
            }

            voidOperation.SubtotalAfter = order.Subtotal ?? 0;
            voidOperation.TaxAfter = order.Tax ?? 0;
            voidOperation.ServiceAfter = order.Service ?? 0;
            voidOperation.GrandTotalAfter = order.GrandTotal ?? 0;
            voidOperation.TotalVoidedAmount = voidedTotalValue;

            await _unitOfWork.Repository<OrderVoid>().AddAsync(voidOperation);
            await _unitOfWork.CompleteAsync();
            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Log.Error(ex, "VoidItemsAsync: Error");
            return false;
        }
    }
}
