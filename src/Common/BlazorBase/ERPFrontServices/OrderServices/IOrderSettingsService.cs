namespace BlazorBase.ERPFrontServices.OrderServices;

public interface IOrderSettingsService
{
    //Task<Order> CreateOrder(Order order);

    public Task<ICollection<OrderSettingToReturnDto>> GetOrderSettingsAsync();
}

