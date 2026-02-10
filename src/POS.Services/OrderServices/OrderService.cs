using POS.Core.Entities.Customer;
using POS.Contract.Dtos.DineIn;
using POS.Core.Services.Contract.OrderServices;
using POS.Core.Specifications.OrderSpecs;
using POS.Core.Services.Contract.AppDateServices;

namespace POS.Services.OrderServices;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppDateService _appDateService;

    public OrderService(IUnitOfWork unitOfWork, IAppDateService appDateService)
    {
        _unitOfWork = unitOfWork;
        _appDateService = appDateService;
    }
    public async Task<Orders?> CreateOrderAsync(Orders order)
    {
        if (order is null)
            return null;

        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                var appDate = await _appDateService.UpdateOrderNumber();
                order.OrderID = appDate.CurrentOrderNumber;
                order.OrderDate = appDate.PosDate.Date.Add(DateTime.Now.TimeOfDay);

                if (order.OrderType == OrderTypes.TakeAway)
                {
                    if (order.TakeawayCustomer is not null)
                    {
                        var existingCustomer = await _unitOfWork.Repository<TakeawayCustomer>()
                            .GetByIdAsync(order.TakeawayCustomer.Id);

                        if (existingCustomer is null)
                        {
                            await _unitOfWork.Repository<TakeawayCustomer>().AddAsync(order.TakeawayCustomer);
                            await _unitOfWork.CompleteAsync();
                            order.TakeawayCustomerId = order.TakeawayCustomer.Id;
                        }
                        else
                        {
                            order.TakeawayCustomerId = existingCustomer.Id;
                        }
                    }
                    else
                    {
                        order.TakeawayCustomerId = null;
                    }
                }
                
                if (string.IsNullOrEmpty(order.MachineName))
                {
                    order.MachineName = Environment.MachineName;
                }

                await _unitOfWork.Repository<Orders>().AddAsync(order);

                if (order.OrderDetails != null && order.OrderDetails.Any())
                {
                    foreach (var item in order.OrderDetails)
                    {
                        await _unitOfWork.Repository<OrderItemsDetails>().AddAsync(item);
                    }
                }

                var result = await _unitOfWork.CompleteAsync();
                if (result <= 0)
                {
                    transaction.Rollback();
                    return null;
                }

                transaction.Commit();
                return order;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "An error occurred while creating the order.");
                return null;
            }
        }

    }

    public async Task<OrderSetting?> GetOrderSettingAsync(OrderTypes orderType)
    {
        var orderSettingSpecs = new OrderSettingSpecs(orderType);
        return await _unitOfWork.Repository<OrderSetting>().GetByIdWithSpecificationAsync(orderSettingSpecs);
    }

    public async Task<IReadOnlyList<OrderSetting>> GetOrderSettingsAsync()
     => await _unitOfWork.Repository<OrderSetting>().GetAllAsync();

    public async Task<OrderSetting?> UpdateOrderSettingAsync(OrderTypes orderType, OrderSetting orderSetting)
    {
        var orderSettingSpecs = new OrderSettingSpecs(orderType);
        var oldOrderSetting = await _unitOfWork.Repository<OrderSetting>().GetByIdWithSpecificationAsync(orderSettingSpecs);

        if (oldOrderSetting is null)
            return null;

        oldOrderSetting.OrderType = oldOrderSetting.OrderType;
        oldOrderSetting.BranchID = oldOrderSetting.BranchID;
        oldOrderSetting.Service = orderSetting.Service != 0 ? orderSetting.Service : oldOrderSetting.Service;
        oldOrderSetting.Tips = orderSetting.Tips != 0 ? orderSetting.Tips : oldOrderSetting.Tips;
        oldOrderSetting.Tax = orderSetting.Tax != 0 ? orderSetting.Tax : oldOrderSetting.Tax;
        oldOrderSetting.SeparateReceiptCount = orderSetting.SeparateReceiptCount != 0 ? orderSetting.SeparateReceiptCount : oldOrderSetting.SeparateReceiptCount;
        oldOrderSetting.CustomerReceiptCount = orderSetting.CustomerReceiptCount != 0 ? orderSetting.CustomerReceiptCount : oldOrderSetting.CustomerReceiptCount;
        oldOrderSetting.ClosingReceiptCount = orderSetting.ClosingReceiptCount != 0 ? orderSetting.ClosingReceiptCount : oldOrderSetting.ClosingReceiptCount;
        oldOrderSetting.FullKitchenReceiptCount = orderSetting.FullKitchenReceiptCount != 0 ? orderSetting.FullKitchenReceiptCount : oldOrderSetting.FullKitchenReceiptCount;
        oldOrderSetting.JobID = orderSetting.JobID != 0 ? orderSetting.JobID : oldOrderSetting.JobID;
        oldOrderSetting.AddServiceToItemPrice = orderSetting.AddServiceToItemPrice;

        _unitOfWork.Repository<OrderSetting>().Update(oldOrderSetting);
        await _unitOfWork.CompleteAsync();
        return oldOrderSetting;
    }

    public async Task<Orders?> GetOrderByOrderIdAsync(int orderId)
    {
        var spec = new OrdersByOrderIdSpecs(orderId);
        return await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationAsync(spec);
    }

    public async Task<bool> VoidOrderAsync(int orderId, string reason, string voidBy, string voidByName)
    {
        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                var spec = new OrdersByOrderIdSpecs(orderId);
                var order = await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationAsync(spec);
                
                if (order is null)
                    return false;

                DateTime voidTime = DateTime.Now;

                order.OrderState = OrderStates.Voided;
                order.ClosingTime = voidTime;
                order.VoidTime = voidTime;
                order.VoidByName = voidByName;
                order.VoidBy = voidBy;
                order.VoidReason = reason;

                decimal totalVoidedValue = 0;
                int totalVoidedCount = 0;

                // Mark all items as voided
                if (order.OrderDetails != null)
                {
                    foreach (var item in order.OrderDetails)
                    {
                        if (item.IsVoided == true) continue;

                        decimal itemValue = item.TotalAmount ?? 0;
                        int itemQty = item.Quantity ?? 0;

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

                        _unitOfWork.Repository<OrderItemsDetails>().Update(item);
                    }
                }

                order.TotalVoid = (order.TotalVoid ?? 0) + totalVoidedValue;
                order.VoidCount = (order.VoidCount ?? 0) + totalVoidedCount;
                order.VoidAmount = (order.VoidAmount ?? 0) + totalVoidedValue;

                _unitOfWork.Repository<Orders>().Update(order);

                var result = await _unitOfWork.CompleteAsync();
                if (result < 0)
                {
                    transaction.Rollback();
                    return false;
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "An error occurred while voiding the order.");
                return false;
            }
        }
    }

    public async Task<bool> VoidOrderItemsAsync(int orderId, List<OrderItemVoidDto> itemsToVoid, string reason, string voidBy, string voidByName)
    {
        if (itemsToVoid == null || !itemsToVoid.Any())
            return false;

        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                var spec = new OrdersByOrderIdSpecs(orderId);
                var order = await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationAsync(spec);
                
                if (order == null) return false;

                DateTime voidTime = DateTime.Now;
                decimal voidedTotalValue = 0;
                int voidedTotalCount = 0;

                foreach (var voidRequest in itemsToVoid)
                {
                    var item = order.OrderDetails?.FirstOrDefault(i => i.Id == voidRequest.OrderItemDetailId);
                    if (item == null || item.Quantity < voidRequest.QuantityToVoid) continue;

                    var unitPrice = (item.TotalAmount ?? 0) / (item.Quantity ?? 1);
                    var valueToVoid = unitPrice * voidRequest.QuantityToVoid;

                    // Track voided details on item
                    item.VoidAmount = (item.VoidAmount ?? 0) + voidRequest.QuantityToVoid;
                    item.TotalVoidAmount = (item.TotalVoidAmount ?? 0) + valueToVoid;
                    item.VoidBy = voidBy;
                    item.VoidByName = voidByName;
                    item.VoidTime = voidTime;
                    item.VoidReason = reason;

                    item.Quantity -= voidRequest.QuantityToVoid;
                    item.TotalAmount -= valueToVoid;
                    
                    if (item.TotalAfterDiscount.HasValue && item.TotalAfterDiscount > 0)
                    {
                        var afterDiscountUnitPrice = item.TotalAfterDiscount.Value / (item.Quantity.Value + voidRequest.QuantityToVoid);
                        item.TotalAfterDiscount -= afterDiscountUnitPrice * voidRequest.QuantityToVoid;
                    }

                    if (item.Quantity <= 0)
                    {
                        item.IsVoided = true;
                    }
                    
                    _unitOfWork.Repository<OrderItemsDetails>().Update(item);

                    voidedTotalValue += valueToVoid;
                    voidedTotalCount += voidRequest.QuantityToVoid;
                }

                // Recalculate order totals
                decimal newSubtotal = order.OrderDetails?.Where(i => i.Quantity > 0).Sum(i => i.TotalAmount ?? 0) ?? 0;
                decimal newService = (newSubtotal * (order.Service ?? 0)) / 100;
                decimal newTax = (newSubtotal * (order.Tax ?? 0)) / 100;
                decimal newGrandTotal = newSubtotal + newService + newTax - (order.Discount ?? 0);

                order.Subtotal = newSubtotal;
                order.GrandTotal = newGrandTotal;
                
                // Update order level void tracking
                order.TotalVoid = (order.TotalVoid ?? 0) + voidedTotalValue;
                order.VoidCount = (order.VoidCount ?? 0) + voidedTotalCount;
                order.VoidAmount = (order.VoidAmount ?? 0) + voidedTotalValue;
                
                // If no items left, void the whole order
                if (order.OrderDetails == null || !order.OrderDetails.Any(i => i.Quantity > 0 && (i.IsVoided == null || i.IsVoided == false)))
                {
                    order.OrderState = OrderStates.Voided;
                    order.VoidTime = voidTime;
                    order.VoidReason = reason;
                    order.VoidByName = voidByName;
                    order.VoidBy = voidBy;
                }

                _unitOfWork.Repository<Orders>().Update(order);
                await _unitOfWork.CompleteAsync();

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "Error voiding items from order {OrderId}", orderId);
                return false;
            }
        }
    }
}