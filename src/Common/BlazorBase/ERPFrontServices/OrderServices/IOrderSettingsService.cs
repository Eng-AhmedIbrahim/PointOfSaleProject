using POS.Contract.Dtos.OrderDtos;

namespace BlazorBase.ERPFrontServices.OrderServices;

public interface IOrderSettingsService
{
    //Task<Order> CreateOrder(Order order);

    public Task<ICollection<OrderSettingToReturnDto>?> GetOrderSettingsAsync(string? computerName = null);
    public Task<OrderDto?> CreateOrderAsync(OrderDto orderDto);
    public Task<OrderDto?> UpdateOrderAsync(OrderDto orderDto);
    public Task<OrderDto?> GetOrderByIdAsync(int orderId);
    public Task<int> IncrementPrintCountAsync(int orderId);
}