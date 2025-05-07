using POS.Contract.Dtos.OrderDtos;

namespace BlazorBase.ERPFrontServices.OrderServices;

public interface IOrderSettingsService
{
    //Task<Order> CreateOrder(Order order);

    public Task<ICollection<OrderSettingToReturnDto>?> GetOrderSettingsAsync();
    public Task<OrderDto?> CreateOrderAsync(OrderDto orderDto);
}