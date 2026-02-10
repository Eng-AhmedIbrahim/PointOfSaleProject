namespace POS.Core.Services.Contract.OrderServices;
using POS.Contract.Dtos.OrderDtos;
using POS.Contract.Dtos.DineIn;
using POS.Core.Entities.OrderEntity;

public interface IOrderService
{
    public Task<Orders?> CreateOrderAsync(Orders order);

    public Task<IReadOnlyList<OrderSetting>> GetOrderSettingsAsync();
    public Task<OrderSetting?> UpdateOrderSettingAsync(OrderTypes orderType, OrderSetting orderSetting);
    public Task<OrderSetting?> GetOrderSettingAsync(OrderTypes orderType);
    public Task<Orders?> GetOrderByOrderIdAsync(int orderId);
    
    // Void methods for TakeAway/Delivery
    public Task<bool> VoidOrderAsync(int orderId, string reason, string voidBy, string voidByName);
    public Task<bool> VoidOrderItemsAsync(int orderId, List<OrderItemVoidDto> itemsToVoid, string reason, string voidBy, string voidByName);
}