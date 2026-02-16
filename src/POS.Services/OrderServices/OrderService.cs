using POS.Core.Entities.Customer;
using POS.Contract.Dtos.DineIn;
using POS.Core.Services.Contract.OrderServices;
using POS.Core.Specifications.OrderSpecs;
using POS.Core.Services.Contract.AppDateServices;
using POS.Core.Repository.Contract;
using Pos.Repository.Data.DataSeed;
using Serilog;
using POS.Core.Entities.OrderEntity;
using POS.Core.Specifications;
using AutoMapper;
using POS.Contract.Dtos.OrderDtos;

namespace POS.Services.OrderServices;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppDateService _appDateService;
    private readonly IMapper _mapper;

    public OrderService(IUnitOfWork unitOfWork, IAppDateService appDateService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _appDateService = appDateService;
        _mapper = mapper;
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

    public async Task<OrderSetting?> GetOrderSettingAsync(OrderTypes orderType, string? computerName = null)
    {
        var orderSettingSpecs = new OrderSettingSpecs(orderType, computerName);
        var settings = await _unitOfWork.Repository<OrderSetting>().GetByIdWithSpecificationAsync(orderSettingSpecs);

        if (settings == null && !string.IsNullOrEmpty(computerName))
        {
            await InitializeOrderSettingsForComputerAsync(computerName);
            settings = await _unitOfWork.Repository<OrderSetting>().GetByIdWithSpecificationAsync(orderSettingSpecs);
        }

        return settings;
    }

    public async Task<IReadOnlyList<OrderSetting>> GetOrderSettingsAsync(string? computerName = null)
    {
        if (string.IsNullOrEmpty(computerName))
            return await _unitOfWork.Repository<OrderSetting>().GetAllAsync();

        var spec = new OrderSettingSpecs(computerName);
        var settings = await _unitOfWork.Repository<OrderSetting>().GetAllWithSpecificationAsync(spec);

        if (settings == null || !settings.Any())
        {
            await InitializeOrderSettingsForComputerAsync(computerName);
            settings = await _unitOfWork.Repository<OrderSetting>().GetAllWithSpecificationAsync(spec);
        }

        return settings.ToList();
    }

    private async Task InitializeOrderSettingsForComputerAsync(string computerName)
    {
        try
        {
            // Get defaults (those with no ComputerName or marked as Default)
            var defaultSpec = new POS.Core.Specifications.BaseSpecifications<OrderSetting>(x => string.IsNullOrEmpty(x.ComputerName) || x.ComputerName == "Default");
            var defaultSettings = await _unitOfWork.Repository<OrderSetting>().GetAllWithSpecificationAsync(defaultSpec);

            if (defaultSettings == null || !defaultSettings.Any())
            {
                // Fallback to JSON if nothing in DB
                var jsonSettings = await PosDbContextDataSeed.GetDataFromJsonFile<OrderSetting>("orderSettings.json");
                if (jsonSettings != null && jsonSettings.Any())
                {
                    foreach (var setting in jsonSettings)
                    {
                        setting.Id = 0;
                        setting.ComputerName = computerName;
                        await _unitOfWork.Repository<OrderSetting>().AddAsync(setting);
                    }
                }
            }
            else
            {
                foreach (var setting in defaultSettings)
                {
                    var newSetting = new OrderSetting
                    {
                        BranchID = setting.BranchID,
                        OrderType = setting.OrderType,
                        OrderStatment = setting.OrderStatment,
                        Service = setting.Service,
                        Tax = setting.Tax,
                        Tips = setting.Tips,
                        JobID = setting.JobID,
                        CustomerReceiptCount = setting.CustomerReceiptCount,
                        FullKitchenReceiptCount = setting.FullKitchenReceiptCount,
                        SeparateReceiptCount = setting.SeparateReceiptCount,
                        ClosingReceiptCount = setting.ClosingReceiptCount,
                        AddServiceToItemPrice = setting.AddServiceToItemPrice,
                        CanCloseWithoutPrint = setting.CanCloseWithoutPrint,
                        DeductCaptainTips = setting.DeductCaptainTips,
                        CaptainTipsAmount = setting.CaptainTipsAmount,
                        ComputerName = computerName
                    };
                    await _unitOfWork.Repository<OrderSetting>().AddAsync(newSetting);
                }
            }
            await _unitOfWork.CompleteAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error initializing order settings for computer {ComputerName}", computerName);
        }
    }

    public async Task<OrderSetting?> UpdateOrderSettingAsync(OrderTypes orderType, OrderSetting orderSetting, string? computerName = null)
    {
        var orderSettingSpecs = new OrderSettingSpecs(orderType, computerName);
        var oldOrderSetting = await _unitOfWork.Repository<OrderSetting>().GetByIdWithSpecificationAsync(orderSettingSpecs);

        if (oldOrderSetting is null)
            return null;

        oldOrderSetting.Service = orderSetting.Service;
        oldOrderSetting.Tips = orderSetting.Tips;
        oldOrderSetting.Tax = orderSetting.Tax;
        oldOrderSetting.OrderStatment = orderSetting.OrderStatment;
        oldOrderSetting.SeparateReceiptCount = orderSetting.SeparateReceiptCount;
        oldOrderSetting.CustomerReceiptCount = orderSetting.CustomerReceiptCount;
        oldOrderSetting.ClosingReceiptCount = orderSetting.ClosingReceiptCount;
        oldOrderSetting.FullKitchenReceiptCount = orderSetting.FullKitchenReceiptCount;
        oldOrderSetting.JobID = orderSetting.JobID;
        oldOrderSetting.AddServiceToItemPrice = orderSetting.AddServiceToItemPrice;
        oldOrderSetting.CanCloseWithoutPrint = orderSetting.CanCloseWithoutPrint;
        oldOrderSetting.DeductCaptainTips = orderSetting.DeductCaptainTips;
        oldOrderSetting.CaptainTipsAmount = orderSetting.CaptainTipsAmount;

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

                        // Null navigation properties to avoid tracking conflicts during update
                        item.MenuSalesItem = null;
                        item.DineInOrder = null;
                        item.Order = null;

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

                    // Null navigation properties to avoid tracking conflicts during update
                    item.MenuSalesItem = null;
                    item.DineInOrder = null;
                    item.Order = null;
                    
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

    public async Task<bool> UpdateOrderStatusAsync(int orderId, OrderStates state)
    {
        var spec = new OrdersByOrderIdSpecs(orderId);
        var order = await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationAsync(spec);
        if (order == null) return false;

        order.OrderState = state;
        _unitOfWork.Repository<Orders>().Update(order);
        return await _unitOfWork.CompleteAsync() > 0;
    }

    public async Task<IReadOnlyList<Orders>?> GetFailedDeliveryOrdersAsync()
    {
        var spec = new BaseSpecifications<Orders>(x => x.OrderType == OrderTypes.Delivery && (x.OrderState == OrderStates.FailedToDeliverToBranch || (x.OrderState == OrderStates.Pending && x.BranchID > 0)));
        // Note: Logic depends on how we identify failed dispatches. 
        // Adding Pending with BranchID > 0 as a fallback for Central mode orders that didn't move.
        return await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
    }

    public async Task<OrderDto?> GetOrderDtoByIdAsync(int id)
    {
        var spec = new OrdersByOrderIdSpecs(id);
        var order = await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationAsync(spec);
        return order != null ? _mapper.Map<OrderDto>(order) : null;
    }

    public async Task<bool> UpdateOrderAsync(Orders order)
    {
        _unitOfWork.Repository<Orders>().Update(order);
        return await _unitOfWork.CompleteAsync() > 0;
    }

    public async Task<Orders?> FullUpdateOrderAsync(Orders order)
    {
        if (order is null || order.OrderID == 0)
            return null;

        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                var spec = new OrdersByOrderIdSpecs(order.OrderID);
                var existingOrder = await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationAsync(spec);
                if (existingOrder == null) return null;

                // Update basic fields
                existingOrder.Subtotal = order.Subtotal;
                existingOrder.Service = order.Service;
                existingOrder.Tax = order.Tax;
                existingOrder.Discount = order.Discount;
                existingOrder.DiscountPercentage = order.DiscountPercentage;
                existingOrder.TotalDiscount = order.TotalDiscount;
                existingOrder.GrandTotal = order.GrandTotal;
                existingOrder.Paid = order.Paid;
                existingOrder.Remain = order.Remain;
                existingOrder.OrderNotice = order.OrderNotice;
                existingOrder.OrderState = order.OrderState;
                existingOrder.PaymentMethod = order.PaymentMethod;
                
                // Delivery specific
                existingOrder.CustomerName = order.CustomerName;
                existingOrder.Phone1 = order.Phone1;
                existingOrder.StreetName = order.StreetName;
                existingOrder.ZoneName = order.ZoneName;
                existingOrder.DeliveryFees = order.DeliveryFees;

                // Clear and Re-add details (simplest way to sync complex order graphs)
                if (existingOrder.OrderDetails != null)
                {
                    foreach (var detail in existingOrder.OrderDetails.ToList())
                    {
                        // Null navigation properties to avoid tracking conflicts during deletion
                        // especially for shared entities like Category/MenuSalesItem
                        detail.MenuSalesItem = null;
                        detail.DineInOrder = null;
                        detail.Order = null;
                        
                        _unitOfWork.Repository<OrderItemsDetails>().Delete(detail);
                    }
                    await _unitOfWork.CompleteAsync();
                    existingOrder.OrderDetails.Clear();
                }

                if (order.OrderDetails != null)
                {
                    // Group and Merge items to prevent duplicates in DB
                    var mergedItems = order.OrderDetails
                        .GroupBy(i => new 
                        { 
                            i.MenuSalesItemId, 
                            i.IsVoided,
                            i.UnitPrice,
                            AttributeKey = string.Join(",", i.OrderItemAttributes?.Select(a => a.AttributeItemId).OrderBy(id => id) ?? Enumerable.Empty<int?>())
                        })
                        .Select(g => 
                        {
                            var first = g.First();
                            if (g.Count() > 1)
                            {
                                first.Quantity = g.Sum(x => x.Quantity ?? 0);
                                first.TotalAmount = g.Sum(x => x.TotalAmount ?? 0);
                                first.TotalAfterDiscount = g.Sum(x => x.TotalAfterDiscount ?? 0);
                                first.TotalDiscountAmount = g.Sum(x => x.TotalDiscountAmount ?? 0);
                                first.TotalVoidAmount = g.Sum(x => x.TotalVoidAmount ?? 0);
                                first.VoidAmount = g.Sum(x => x.VoidAmount ?? 0);
                                first.TotalDiscountPrice = g.Sum(x => x.TotalDiscountPrice ?? 0);
                            }
                            return first;
                        }).ToList();

                    foreach (var item in mergedItems)
                    {
                        item.OrderId = existingOrder.Id;
                        item.Id = 0; // Ensure it's treated as a new insert
                        // Clear IDs from attributes to prevent identity insert errors
                        if (item.OrderItemAttributes != null)
                        {
                            foreach (var attr in item.OrderItemAttributes)
                            {
                                attr.Id = 0;
                                attr.OrderItemId = 0;
                            }
                        }
                        await _unitOfWork.Repository<OrderItemsDetails>().AddAsync(item);
                    }
                }

                _unitOfWork.Repository<Orders>().Update(existingOrder);
                await _unitOfWork.CompleteAsync();

                transaction.Commit();

                // Re-fetch to get all merged items and includes for the response
                return await GetOrderByOrderIdAsync(existingOrder.OrderID);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "An error occurred while updating the order {OrderId}.", order.OrderID);
                return null;
            }
        }
    }
    public async Task<IReadOnlyList<Orders>> GetOrdersByCustomerPhoneAsync(string phoneNumber)
    {
        var spec = new OrdersByCustomerPhoneSpecs(phoneNumber);
        return await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
    }

    public async Task<int> IncrementPrintCountAsync(int orderId)
    {
        try
        {
            var spec = new BaseSpecifications<Orders>(o => o.OrderID == orderId);
            var order = await _unitOfWork.Repository<Orders>().GetByIdWithSpecificationAsync(spec);
            
            if (order == null) return 0;

            order.PrintCount = (order.PrintCount ?? 0) + 1;
            
            _unitOfWork.Repository<Orders>().Update(order);
            await _unitOfWork.CompleteAsync();
            
            return order.PrintCount ?? 0;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error incrementing print count for order {OrderId}", orderId);
            return 0;
        }
    }
}