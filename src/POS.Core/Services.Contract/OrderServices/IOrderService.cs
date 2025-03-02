namespace POS.Core.Services.Contract.OrderServices;

public interface IOrderService
{
    public Task<Orders?> CreateOrderAsync(Orders order);
}
