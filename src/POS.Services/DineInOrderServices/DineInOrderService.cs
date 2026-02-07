namespace POS.Services.DineInOrderServices;

public class DineInOrderService : IDineInOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderTrackService _orderTrackService;
    private readonly IAppDateService _appDateService;

    public DineInOrderService(IUnitOfWork unitOfWork, 
        IOrderTrackService orderTrackService, 
        IAppDateService appDateService)
    {
        _unitOfWork = unitOfWork;
        _orderTrackService = orderTrackService;
        _appDateService = appDateService;
    }

    private async Task RecalculateOrderTotalsAsync(Orders order, OrderSetting? settings = null)
    {
        if (settings == null)
        {
            settings = await _unitOfWork.Repository<OrderSetting>().GetByIdWithSpecificationAsync(new POS.Core.Specifications.OrderSpecs.OrderSettingSpecs(OrderTypes.DineIn));
        }

        if (order.OrderDetails == null) return;

        // 1. Calculate Subtotal from active items
        // Use ReferenceEqualityComparer to ensure we don't sum the same object twice even if it's in the list multiple times
        var activeItems = order.OrderDetails
            .Where(i => i.Quantity > 0 && (i.IsVoided == null || i.IsVoided == false))
            .Distinct(ReferenceEqualityComparer.Instance)
            .Cast<OrderItemsDetails>();

        decimal subtotal = activeItems.Sum(i => i.TotalAmount ?? 0);
        
        order.Subtotal = subtotal;

        // 2. Calculate Service and Tax
        decimal serviceRate = settings?.Service ?? 0;
        decimal taxRate = settings?.Tax ?? 0;

        decimal serviceAmount = (subtotal * serviceRate) / 100;
        decimal taxAmount = (subtotal * taxRate) / 100;

        order.Service = serviceAmount;
        order.Tax = taxAmount;

        // 3. Interim Total (After Tax and Service)
        decimal interimTotal = subtotal + serviceAmount + taxAmount;

        // 4. Calculate Order Discount
        decimal orderDiscountAmount = 0;
        if (order.DiscountPercentage > 0)
        {
            orderDiscountAmount = (interimTotal * (order.DiscountPercentage ?? 0)) / 100;
            order.Discount = orderDiscountAmount;
        }
        else if (order.Discount > 0)
        {
            orderDiscountAmount = order.Discount ?? 0;
        }

        order.TotalDiscount = orderDiscountAmount; 

        // 5. Grand Total
        decimal grandTotal = interimTotal - orderDiscountAmount;
        
        // Round to 0.5 as per CartService logic
        order.GrandTotal = Math.Round(grandTotal * 2, MidpointRounding.AwayFromZero) / 2;
    }

    private bool AreItemsSame(OrderItemsDetails item1, OrderItemsDetails item2)
    {
        if (item1.MenuSalesItemId != item2.MenuSalesItemId) return false;
        
        // Comparison for denormalized values for safety
        if (item1.UnitPrice != item2.UnitPrice) return false;

        // Check attributes matching
        var attrs1 = item1.OrderItemAttributes?.Select(a => a.AttributeItemId).OrderBy(id => id).ToList() ?? new List<int?>();
        var attrs2 = item2.OrderItemAttributes?.Select(a => a.AttributeItemId).OrderBy(id => id).ToList() ?? new List<int?>();
        
        if (attrs1.Count != attrs2.Count) return false;
        for (int i = 0; i < attrs1.Count; i++)
        {
            if (attrs1[i] != attrs2[i]) return false;
        }
        
        return true;
    }

    public async Task<Orders?> CreateDineInOrderAsync(Orders order)
    {
        if (order is null)
            return null;

        // Allow multiple open orders for a table to support splitting
        // The check below is removed to allow creating multiple orders for the same table
        /*
        var existingOrder = await GetDineInOrderByTableIdAsync(order.TableID ?? 0, "Open");
        if (existingOrder != null)
        {
            Log.Warning("Attempted to create a duplicate DineIn order for Table {TableId}. Current OrderID: {OrderId}", order.TableID, order.OrderID);
            return existingOrder;
        }
        */

        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                var appDate = await _appDateService.UpdateOrderNumber();
                order.OrderID = appDate.CurrentOrderNumber;
                order.OrderDate = appDate.PosDate.Date.Add(DateTime.Now.TimeOfDay);
                order.OrderType = OrderTypes.DineIn;
                order.OrderState = OrderStates.Pending;
                if (string.IsNullOrEmpty(order.MachineName))
                {
                    order.MachineName = Environment.MachineName;
                }

                // Apply captain tips deduction if enabled in settings
                var dineInSettings = await _unitOfWork.Repository<OrderSetting>().GetByIdWithSpecificationAsync(new POS.Core.Specifications.OrderSpecs.OrderSettingSpecs(OrderTypes.DineIn));
                if (dineInSettings != null && dineInSettings.DeductCaptainTips == true)
                {
                    order.CaptainTipsDeduction = dineInSettings.CaptainTipsAmount;
                }
                
                await RecalculateOrderTotalsAsync(order, dineInSettings);

                if (order.OrderDetails != null && order.OrderDetails.Any())
                {
                    foreach (var item in order.OrderDetails)
                    {
                        item.OrderType = OrderTypes.DineIn;
                        item.MenuSalesItem = null; // Prevent identity conflict

                        // Ensure denormalized fields are populated
                        if (item.MenuSalesItemId.HasValue && (string.IsNullOrEmpty(item.ItemName) || (item.UnitPrice ?? 0) == 0))
                        {
                            var product = await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(item.MenuSalesItemId.Value);
                            if (product != null)
                            {
                                item.ItemName = string.IsNullOrEmpty(item.ItemName) ? product.EnglishName : item.ItemName;
                                item.ItemNameAr = string.IsNullOrEmpty(item.ItemNameAr) ? product.ArabicName : item.ItemNameAr;
                                item.UnitPrice = (item.UnitPrice ?? 0) == 0 ? product.Price : item.UnitPrice;
                                item.CategoryId = item.CategoryId ?? product.CategoryId;
                            }
                        }
                        
                        // Ensure OrderId is not set yet for new items to avoid confusion
                        item.OrderId = 0;
                    }
                }
                
                // Add the whole order graph (order + details)
                await _unitOfWork.Repository<Orders>().AddAsync(order);

                var result = await _unitOfWork.CompleteAsync();
                if (result <= 0)
                {
                    transaction.Rollback();
                    return null;
                }

                // Track the order creation
                await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                {
                    OrderId = order.OrderID,
                    OrderType = "DineIn",
                    Action = "Created",
                    UserName = order.CashierName,
                    UserId = order.CashierID,
                    MachineName = Environment.MachineName,
                    TableId = order.TableID,
                    TableName = order.TableName,
                    Details = $"Order created with {order.OrderDetails?.Count ?? 0} items. Total: {order.GrandTotal}",
                    ActionDateTime = DateTime.Now
                });

                transaction.Commit();
                return order;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "An error occurred while creating the DineIn order in Orders table.");
                return null;
            }
        }
    }

    public async Task<Orders?> UpdateDineInOrderAsync(Orders order)
    {
        if (order is null)
            return null;

        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                var existingOrder = await _unitOfWork.Repository<Orders>().GetByIdAsync(order.Id);
                if (existingOrder is null)
                    return null;

                // Update order properties
                existingOrder.Subtotal = order.Subtotal;
                existingOrder.Tax = order.Tax;
                existingOrder.Service = order.Service;
                existingOrder.Discount = order.Discount;
                existingOrder.DiscountPercentage = order.DiscountPercentage;
                existingOrder.DiscountType = order.DiscountType;
                existingOrder.DiscountReason = order.DiscountReason;
                existingOrder.TotalDiscount = order.TotalDiscount;
                
                // Recalculate based on updated properties if needed, 
                // but usually UpdateDineInOrderAsync is called with full calculated object from UI.
                // To be safe, let's ensure GrandTotal is updated if Subtotal changed.
                await RecalculateOrderTotalsAsync(existingOrder);

                existingOrder.OrderNotice = order.OrderNotice;
                existingOrder.CustomerName = order.CustomerName;
                existingOrder.Phone1 = order.Phone1;
                existingOrder.PaymentMethod = order.PaymentMethod;

                _unitOfWork.Repository<Orders>().Update(existingOrder);

                var result = await _unitOfWork.CompleteAsync();
                if (result <= 0)
                {
                    transaction.Rollback();
                    return null;
                }

                // Track the order update
                await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                {
                    OrderId = order.OrderID,
                    OrderType = "DineIn",
                    Action = "Updated",
                    UserName = order.CashierName,
                    UserId = order.CashierID,
                    MachineName = Environment.MachineName,
                    TableId = order.TableID,
                    TableName = order.TableName,
                    Details = $"Order updated. Total: {order.GrandTotal}",
                    ActionDateTime = DateTime.Now
                });

                transaction.Commit();
                return existingOrder;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "An error occurred while updating the DineIn order.");
                return null;
            }
        }
    }

    public async Task<Orders?> GetDineInOrderByIdAsync(int orderId)
    {
        var spec = new DineInOrderByIdSpec(orderId);
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
        return orders.FirstOrDefault();
    }

    public async Task<Orders?> GetDineInOrderByTableIdAsync(int tableId, string state = "Open")
    {
        var spec = new DineInOrderByTableIdSpec(tableId, state);
        var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
        return orders.OrderByDescending(o => o.OrderDate).FirstOrDefault();
    }

    public async Task<IReadOnlyList<Orders>> GetOpenOrdersByTableIdAsync(int tableId)
    {
        var spec = new DineInOrderByTableIdSpec(tableId, "Open");
        return await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
    }

    public async Task<IReadOnlyList<Orders>> GetAllOpenDineInOrdersAsync()
    {
        var spec = new DineInOrderByStateSpec("Open");
        return await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
    }

    public async Task<bool> CloseDineInOrderAsync(int orderId)
    {
        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                var orders = await _unitOfWork.Repository<Orders>()
                    .GetAllWithSpecificationAsync(new DineInOrderByIdSpec(orderId));
                
                var order = orders.FirstOrDefault();
                if (order is null)
                    return false;

                order.OrderState = OrderStates.Completed;
                order.ClosingTime = DateTime.Now;

                _unitOfWork.Repository<Orders>().Update(order);

                var result = await _unitOfWork.CompleteAsync();
                if (result <= 0)
                {
                    transaction.Rollback();
                    return false;
                }

                // Track the order closure
                await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                {
                    OrderId = order.OrderID,
                    OrderType = "DineIn",
                    Action = "Closed",
                    UserName = order.CashierName,
                    UserId = order.CashierID,
                    MachineName = Environment.MachineName,
                    TableId = order.TableID,
                    TableName = order.TableName,
                    Details = $"Order closed. Total: {order.GrandTotal}",
                    ActionDateTime = DateTime.Now
                });

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "An error occurred while closing the DineIn order.");
                return false;
            }
        }
    }

    public async Task<bool> VoidDineInOrderAsync(int orderId)
    {
        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                var orders = await _unitOfWork.Repository<Orders>()
                    .GetAllWithSpecificationAsync(new DineInOrderByIdSpec(orderId));
                
                var order = orders.FirstOrDefault();
                if (order is null)
                    return false;

                order.OrderState = OrderStates.Voided;
                order.ClosingTime = DateTime.Now;
                order.VoidTime = DateTime.Now;
                order.VoidByName = order.CashierName;
                order.VoidBy = order.CashierID;

                _unitOfWork.Repository<Orders>().Update(order);

                var result = await _unitOfWork.CompleteAsync();
                if (result <= 0)
                {
                    transaction.Rollback();
                    return false;
                }

                // Track the void action
                await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                {
                    OrderId = order.OrderID,
                    OrderType = "DineIn",
                    Action = "Voided",
                    UserName = order.CashierName,
                    UserId = order.CashierID,
                    MachineName = Environment.MachineName,
                    TableId = order.TableID,
                    TableName = order.TableName,
                    Details = $"Order voided. Total: {order.GrandTotal}",
                    ActionDateTime = DateTime.Now
                });

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "An error occurred while voiding the DineIn order.");
                return false;
            }
        }
    }

    public async Task<bool> AddItemsToDineInOrderAsync(int orderId, List<OrderItemsDetails> items)
    {
        if (items is null || !items.Any())
            return false;

        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                decimal itemsTotal = 0;
                var spec = new DineInOrderByIdSpec(orderId);
                var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
                var order = orders.FirstOrDefault();
                
                if (order is null)
                    return false;

                foreach (var newItem in items)
                {
                    newItem.OrderType = OrderTypes.DineIn;

                    // Ensure denormalized fields are populated
                    if (newItem.MenuSalesItemId.HasValue && (string.IsNullOrEmpty(newItem.ItemName) || (newItem.UnitPrice ?? 0) == 0))
                    {
                        var product = await _unitOfWork.Repository<MenuSalesItems>().GetByIdAsync(newItem.MenuSalesItemId.Value);
                        if (product != null)
                        {
                            newItem.ItemName = string.IsNullOrEmpty(newItem.ItemName) ? product.EnglishName : newItem.ItemName;
                            newItem.ItemNameAr = string.IsNullOrEmpty(newItem.ItemNameAr) ? product.ArabicName : newItem.ItemNameAr;
                            newItem.UnitPrice = (newItem.UnitPrice ?? 0) == 0 ? product.Price : newItem.UnitPrice;
                            newItem.CategoryId = newItem.CategoryId ?? product.CategoryId;
                        }
                    }
                    
                    var existingItem = order.OrderDetails?.FirstOrDefault(i => AreItemsSame(i, newItem));
                    if (existingItem != null)
                    {
                        existingItem.Quantity = (existingItem.Quantity ?? 0) + (newItem.Quantity ?? 0);
                        existingItem.TotalAmount = (existingItem.TotalAmount ?? 0) + (newItem.TotalAmount ?? 0);
                        
                        // Update denormalized data on existing item if missing
                        existingItem.ItemName ??= newItem.ItemName;
                        existingItem.ItemNameAr ??= newItem.ItemNameAr;
                        existingItem.UnitPrice ??= newItem.UnitPrice;
                        
                        existingItem.MenuSalesItem = null;
                        _unitOfWork.Repository<OrderItemsDetails>().Update(existingItem);
                    }
                    else
                    {
                        newItem.Order = order; // Link via navigation property
                        newItem.OrderId = order.Id;
                        newItem.MenuSalesItem = null;
                        await _unitOfWork.Repository<OrderItemsDetails>().AddAsync(newItem);
                        
                        order.OrderDetails ??= new List<OrderItemsDetails>();
                        if (!order.OrderDetails.Any(i => i == newItem))
                        {
                            order.OrderDetails.Add(newItem);
                        }
                    }
                    itemsTotal += newItem.TotalAmount ?? 0;
                }

                // Update order totals using robust calculation
                await RecalculateOrderTotalsAsync(order);
                
                _unitOfWork.Repository<Orders>().Update(order);

                var result = await _unitOfWork.CompleteAsync();
                if (result <= 0)
                {
                    transaction.Rollback();
                    return false;
                }

                // Track the item addition
                await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                {
                    OrderId = order.OrderID,
                    OrderType = "DineIn",
                    Action = "ItemsAdded",
                    UserName = order.CashierName,
                    UserId = order.CashierID,
                    MachineName = Environment.MachineName,
                    TableId = order.TableID,
                    TableName = order.TableName,
                    Details = $"{items.Count} items added to order. Added Total: {itemsTotal}. New GrandTotal: {order.GrandTotal}",
                    ActionDateTime = DateTime.Now
                });

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "An error occurred while adding items to the DineIn order.");
                return false;
            }
        }
    }

    public async Task<bool> UpdateDineInOrderDiscountAsync(int orderId, decimal? discountAmount, decimal? discountPercentage, string? discountType, string? discountReason)
    {
        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                var order = await _unitOfWork.Repository<Orders>().GetByIdAsync(orderId);
                if (order is null)
                    return false;

                order.Discount = discountAmount;
                order.DiscountPercentage = discountPercentage;
                order.DiscountType = discountType;
                order.DiscountReason = discountReason;

                await RecalculateOrderTotalsAsync(order);

                _unitOfWork.Repository<Orders>().Update(order);

                var result = await _unitOfWork.CompleteAsync();
                if (result <= 0)
                {
                    transaction.Rollback();
                    return false;
                }

                // Track the discount update
                await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                {
                    OrderId = order.OrderID,
                    OrderType = "DineIn",
                    Action = "DiscountApplied",
                    UserName = order.CashierName,
                    UserId = order.CashierID,
                    MachineName = Environment.MachineName,
                    TableId = order.TableID,
                    TableName = order.TableName,
                    Details = $"Discount applied: {discountType} - Amount: {discountAmount}, Percentage: {discountPercentage}%, Reason: {discountReason}",
                    ActionDateTime = DateTime.Now
                });

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "An error occurred while updating discount for the DineIn order.");
                return false;
            }
        }
    }

    public async Task<bool> TransferDineInOrderAsync(int orderId, int newTableId, string newTableName)
    {
        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                var order = await _unitOfWork.Repository<Orders>().GetByIdAsync(orderId);
                if (order is null)
                    return false;

                var oldTableName = order.TableName;
                var oldTableId = order.TableID;

                order.TableID = newTableId;
                order.TableName = newTableName;

                _unitOfWork.Repository<Orders>().Update(order);
                await _unitOfWork.CompleteAsync();

                await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                {
                    OrderId = order.OrderID,
                    OrderType = "DineIn",
                    Action = "Transferred",
                    UserName = order.CashierName,
                    UserId = order.CashierID,
                    MachineName = Environment.MachineName,
                    TableId = newTableId,
                    TableName = newTableName,
                    Details = $"Order transferred from Table {oldTableName} (ID: {oldTableId}) to Table {newTableName} (ID: {newTableId})",
                    ActionDateTime = DateTime.Now
                });

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "Error transferring DineIn order");
                return false;
            }
        }
    }

    public async Task<bool> MergeDineInOrdersAsync(int primaryOrderId, List<int> secondaryOrderIds)
    {
        if (secondaryOrderIds == null || !secondaryOrderIds.Any())
            return false;

        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                var primaryOrderSpec = new DineInOrderByIdSpec(primaryOrderId);
                var primaryOrders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(primaryOrderSpec);
                var primaryOrder = primaryOrders.FirstOrDefault();
                
                if (primaryOrder == null)
                    return false;

                foreach (var secId in secondaryOrderIds)
                {
                    var spec = new DineInOrderByIdSpec(secId);
                    var secOrders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
                    var secOrder = secOrders.FirstOrDefault();
                    
                    if (secOrder == null) continue;

                    // Move items or merge if same
                    if (secOrder.OrderDetails != null)
                    {
                        foreach (var secItem in secOrder.OrderDetails)
                        {
                            // Clear navigation property to avoid identity conflict in EF tracking
                            secItem.MenuSalesItem = null;

                            var primaryItem = primaryOrder.OrderDetails?.FirstOrDefault(i => AreItemsSame(i, secItem));
                            if (primaryItem != null)
                            {
                                primaryItem.MenuSalesItem = null;
                                primaryItem.Quantity += secItem.Quantity;
                                primaryItem.TotalAmount += secItem.TotalAmount;
                                _unitOfWork.Repository<OrderItemsDetails>().Update(primaryItem);
                                _unitOfWork.Repository<OrderItemsDetails>().Delete(secItem);
                            }
                            else
                            {
                                secItem.OrderId = primaryOrderId;
                                secItem.MenuSalesItem = null;
                                if (primaryOrder.OrderDetails != null && !primaryOrder.OrderDetails.Any(i => i.Id == secItem.Id))
                                {
                                    primaryOrder.OrderDetails.Add(secItem);
                                }
                            }
                        }
                    }

                    // Void secondary order
                    secOrder.OrderState = OrderStates.Voided;
                    secOrder.VoidReason = $"Merged into Order ID: {primaryOrder.OrderID}";
                    secOrder.VoidTime = DateTime.Now;
                    secOrder.VoidByName = primaryOrder.CashierName;
                    _unitOfWork.Repository<Orders>().Update(secOrder);
                }

                await RecalculateOrderTotalsAsync(primaryOrder);
                _unitOfWork.Repository<Orders>().Update(primaryOrder);
                await _unitOfWork.CompleteAsync();

                await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                {
                    OrderId = primaryOrder.OrderID,
                    OrderType = "DineIn",
                    Action = "Merged",
                    UserName = primaryOrder.CashierName,
                    UserId = primaryOrder.CashierID,
                    MachineName = Environment.MachineName,
                    TableId = primaryOrder.TableID,
                    TableName = primaryOrder.TableName,
                    Details = $"Orders {string.Join(", ", secondaryOrderIds)} merged into this order. New GrandTotal: {primaryOrder.GrandTotal}",
                    ActionDateTime = DateTime.Now
                });

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "Error merging DineIn orders");
                return false;
            }
        }
    }

    public async Task<bool> SplitDineInOrderAsync(int sourceOrderId, List<SplitTargetDto> targets)
    {
        if (targets == null || !targets.Any())
            return false;

        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                var sourceOrderSpec = new DineInOrderByIdSpec(sourceOrderId);
                var sourceOrders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(sourceOrderSpec);
                var sourceOrder = sourceOrders.FirstOrDefault();
                
                if (sourceOrder == null) return false;

                decimal totalMovedFromSource = 0;

                foreach (var targetDto in targets)
                {
                    if (targetDto.ItemsToMove == null || !targetDto.ItemsToMove.Any()) continue;

                    // Get target table name
                    var targetTableResult = await _unitOfWork.Repository<Table>().GetByIdAsync(targetDto.TargetTableId);
                    if (targetTableResult == null) continue;

                    // Get a new OrderID for each split
                    var appDate = await _appDateService.UpdateOrderNumber();
                    int newOrderId = appDate.CurrentOrderNumber;

                    // Create target order (always new for splits as per requirements)
                    var targetOrder = new Orders
                    {
                        OrderDate = appDate.PosDate.Date.Add(DateTime.Now.TimeOfDay),
                        OrderID = newOrderId,
                        OrderType = OrderTypes.DineIn,
                        OrderState = OrderStates.Pending,
                        TableID = targetDto.TargetTableId,
                        TableName = string.IsNullOrEmpty(targetDto.Label) 
                                    ? targetTableResult.TableName 
                                    : $"{targetTableResult.TableName} - {targetDto.Label}",
                        BranchID = sourceOrder.BranchID,
                        BranchName = sourceOrder.BranchName,
                        CashierID = sourceOrder.CashierID,
                        CashierName = sourceOrder.CashierName,
                        GrandTotal = 0,
                        Subtotal = 0,
                        // Copy Captain Order from source order
                        WaiterID = sourceOrder.WaiterID,
                        WaiterName = sourceOrder.WaiterName,
                        OrderDetails = new List<OrderItemsDetails>()
                    };

                    await _unitOfWork.Repository<Orders>().AddAsync(targetOrder);
                    // Removed intermediate CompleteAsync() - will link items via navigation property instead
                    
                    decimal targetMovedTotal = 0;

                    foreach (var splitRequest in targetDto.ItemsToMove)
                    {
                        var item = sourceOrder.OrderDetails?.FirstOrDefault(i => i.Id == splitRequest.OrderItemDetailId);
                        if (item == null || (item.Quantity ?? 0) < splitRequest.QuantityToMove) continue;

                        var unitPrice = (item.TotalAmount ?? 0) / (item.Quantity ?? 1);
                        var valueToMove = unitPrice * splitRequest.QuantityToMove;

                        // Update source item
                        item.Quantity -= splitRequest.QuantityToMove;
                        item.TotalAmount -= valueToMove;
                        
                        item.MenuSalesItem = null;
                        if (item.Quantity <= 0)
                            _unitOfWork.Repository<OrderItemsDetails>().Delete(item);
                        else
                            _unitOfWork.Repository<OrderItemsDetails>().Update(item);

                        // Add to target
                        var newItem = new OrderItemsDetails
                        {
                            OrderType = OrderTypes.DineIn,
                            MenuSalesItemId = item.MenuSalesItemId,
                            ItemName = item.ItemName,
                            ItemNameAr = item.ItemNameAr,
                            CategoryId = item.CategoryId,
                            CategoryName = item.CategoryName,
                            UnitPrice = item.UnitPrice,
                            Quantity = splitRequest.QuantityToMove,
                            TotalAmount = valueToMove,
                            OrderItemAttributes = item.OrderItemAttributes?.Select(a => new OrderItemAttributes
                            {
                                AttributeItemId = a.AttributeItemId,
                                AttributeName = a.AttributeName
                            }).ToList()
                        };
                        
                        newItem.Order = targetOrder; // Link via navigation property
                        await _unitOfWork.Repository<OrderItemsDetails>().AddAsync(newItem);
                        
                        targetOrder.OrderDetails.Add(newItem);
                        
                        targetMovedTotal += valueToMove;
                    }

                    // Target order gets the same discount settings from source
                    targetOrder.DiscountPercentage = sourceOrder.DiscountPercentage;
                    targetOrder.Discount = sourceOrder.Discount; // Ensure fixed amount discount is also copied
                    targetOrder.DiscountType = sourceOrder.DiscountType;
                    targetOrder.DiscountReason = sourceOrder.DiscountReason;

                    await RecalculateOrderTotalsAsync(targetOrder);
                    _unitOfWork.Repository<Orders>().Update(targetOrder);

                    totalMovedFromSource += targetMovedTotal;

                    // Track target order creation
                    await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                    {
                        OrderId = targetOrder.OrderID,
                        OrderType = "DineIn",
                        Action = "SplitCreated",
                        UserName = sourceOrder.CashierName,
                        UserId = sourceOrder.CashierID,
                        MachineName = Environment.MachineName,
                        TableId = targetOrder.TableID,
                        TableName = targetOrder.TableName,
                        Details = $"Order created via split from Order #{sourceOrder.OrderID}. Total: {targetOrder.GrandTotal}",
                        ActionDateTime = DateTime.Now
                    });
                }

                // Update source order totals
                await RecalculateOrderTotalsAsync(sourceOrder);

                if (sourceOrder.Subtotal <= 0 || !sourceOrder.OrderDetails!.Any(i => i.Quantity > 0))
                {
                    sourceOrder.OrderState = OrderStates.Voided;
                    sourceOrder.VoidReason = "Completely split into other orders";
                    sourceOrder.VoidTime = DateTime.Now;
                    sourceOrder.VoidByName = sourceOrder.CashierName;
                }

                _unitOfWork.Repository<Orders>().Update(sourceOrder);
                await _unitOfWork.CompleteAsync();

                // Track source order update
                await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                {
                    OrderId = sourceOrder.OrderID,
                    OrderType = "DineIn",
                    Action = "Split",
                    UserName = sourceOrder.CashierName,
                    UserId = sourceOrder.CashierID,
                    MachineName = Environment.MachineName,
                    TableId = sourceOrder.TableID,
                    TableName = sourceOrder.TableName,
                    Details = $"Split into {targets.Count} targets. Total moved: {totalMovedFromSource}",
                    ActionDateTime = DateTime.Now
                });

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "Error splitting DineIn order");
                return false;
            }
        }
    }

    public async Task<bool> VoidDineInItemsAsync(int orderId, List<OrderItemVoidDto> itemsToVoid, string reason, string voidBy)
    {
        if (itemsToVoid == null || !itemsToVoid.Any())
            return false;

        using (var transaction = _unitOfWork.BeginTransaction())
        {
            try
            {
                var spec = new DineInOrderByIdSpec(orderId);
                var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
                var order = orders.FirstOrDefault();
                
                if (order == null) return false;

                decimal voidedTotal = 0;

                foreach (var voidRequest in itemsToVoid)
                {
                    var item = order.OrderDetails?.FirstOrDefault(i => i.Id == voidRequest.OrderItemDetailId);
                    if (item == null || item.Quantity < voidRequest.QuantityToVoid) continue;

                    var unitPrice = (item.TotalAmount ?? 0) / (item.Quantity ?? 1);
                    var valueToVoid = unitPrice * voidRequest.QuantityToVoid;

                    // Track voided amount
                    item.VoidAmount = (item.VoidAmount ?? 0) + voidRequest.QuantityToVoid;
                    item.Quantity -= voidRequest.QuantityToVoid;
                    item.TotalAmount -= valueToVoid;
                    
                    item.MenuSalesItem = null;
                    if (item.Quantity <= 0)
                    {
                        item.IsVoided = true;
                        _unitOfWork.Repository<OrderItemsDetails>().Update(item);
                        // We keep the record but marked as voided to track the void amount as requested
                    }
                    else
                    {
                        _unitOfWork.Repository<OrderItemsDetails>().Update(item);
                    }

                    voidedTotal += valueToVoid;
                }

                // Update order totals
                await RecalculateOrderTotalsAsync(order);
                
                // If no items left, void the whole order
                if (order.OrderDetails == null || !order.OrderDetails.Any(i => i.Quantity > 0 && (i.IsVoided == null || i.IsVoided == false)))
                {
                    order.OrderState = OrderStates.Voided;
                    order.VoidTime = DateTime.Now;
                    order.VoidReason = reason;
                    order.VoidByName = voidBy;
                }

                _unitOfWork.Repository<Orders>().Update(order);
                await _unitOfWork.CompleteAsync();

                await _orderTrackService.TrackOrderActionAsync(new OrderTrack
                {
                    OrderId = order.OrderID,
                    OrderType = "DineIn",
                    Action = "ItemsVoided",
                    UserName = voidBy,
                    UserId = voidBy, // Assuming voidBy is name/id for track
                    MachineName = Environment.MachineName,
                    TableId = order.TableID,
                    TableName = order.TableName,
                    Details = $"Voided {itemsToVoid.Count} items (Total: {voidedTotal}). Reason: {reason}",
                    ActionDateTime = DateTime.Now
                });

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Log.Error(ex, "Error voiding DineIn order items");
                return false;
            }
        }
    }
    public async Task<int> IncrementPrintCountAsync(int orderId)
    {
        try
        {
            var orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(new DineInOrderByIdSpec(orderId));
            var order = orders.FirstOrDefault();
            
            if (order == null)
            {
                // Fallback: search by display OrderID if PK search fails
                var spec = new DineInOrderByOrderIdSpec(orderId);
                orders = await _unitOfWork.Repository<Orders>().GetAllWithSpecificationAsync(spec);
                order = orders.FirstOrDefault();
            }

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
