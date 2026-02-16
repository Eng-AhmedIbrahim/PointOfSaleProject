namespace POS.Core.Services.Contract.OrderServices;
using POS.Contract.Dtos.OrderDtos;
using POS.Contract.Dtos.DineIn;
using POS.Core.Entities.OrderEntity;

public interface IOrderService
{
    public Task<Orders?> CreateOrderAsync(Orders order);

    public Task<IReadOnlyList<OrderSetting>> GetOrderSettingsAsync(string? computerName = null);
    public Task<OrderSetting?> UpdateOrderSettingAsync(OrderTypes orderType, OrderSetting orderSetting, string? computerName = null);
    public Task<OrderSetting?> GetOrderSettingAsync(OrderTypes orderType, string? computerName = null);
    public Task<Orders?> GetOrderByOrderIdAsync(int orderId);
    
    // Void methods for TakeAway/Delivery
    public Task<bool> VoidOrderAsync(int orderId, string reason, string voidBy, string voidByName);
    public Task<bool> VoidOrderItemsAsync(int orderId, List<OrderItemVoidDto> itemsToVoid, string reason, string voidBy, string voidByName);
    public Task<bool> UpdateOrderStatusAsync(int orderId, OrderStates state);
    public Task<IReadOnlyList<Orders>?> GetFailedDeliveryOrdersAsync();
    public Task<OrderDto?> GetOrderDtoByIdAsync(int id);
    public Task<bool> UpdateOrderAsync(Orders order);
    public Task<Orders?> FullUpdateOrderAsync(Orders order);
    public Task<IReadOnlyList<Orders>> GetOrdersByCustomerPhoneAsync(string phoneNumber);
    public Task<int> IncrementPrintCountAsync(int orderId);
}