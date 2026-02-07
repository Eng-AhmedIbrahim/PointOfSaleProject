namespace POS.Core.Services.Contract.OrderServices;

public interface IOrderService
{
    public Task<Orders?> CreateOrderAsync(Orders order);

    public Task<IReadOnlyList<OrderSetting>> GetOrderSettingsAsync();
    public Task<OrderSetting?> UpdateOrderSettingAsync(OrderTypes orderType, OrderSetting orderSetting);
    public Task<OrderSetting?> GetOrderSettingAsync(OrderTypes orderType);
    public Task<Orders?> GetOrderByOrderIdAsync(int orderId);
}