using POS.Core.Entities.Customer;
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
}