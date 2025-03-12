namespace POS.Core.Services.Contract.PrinterServices;

public interface IPrinterServices 
{
    public Task<KitchenType?> CreatePrinterAsync(KitchenType printer);
    public Task<List<KitchenType>?> GetKitchenTypesAsync();
    public Task<KitchenType?> GetKitchenByIdAsync(int kitchenId);
    public Task<KitchenType?> UpdateKitchenTypesAsync(KitchenType printer);

    public Task<OrderSetting?> CreateOrderSettingAsync(OrderSetting orderSetting);
    public Task<OrderSetting?> UpdateOrderSettingAsync(OrderSetting orderSetting);
    public Task<List<OrderSetting>?> GetOrderSettingsAsync();
    public Task<OrderSetting?> GetOrderSettingByIdAsync(int orderSettingId);
}
