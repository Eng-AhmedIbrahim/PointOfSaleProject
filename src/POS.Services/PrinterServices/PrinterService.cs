using POS.Core.Entities.Kitchen;
using POS.Core.Entities.OrderEntity;
using POS.Core.Services.Contract.PrinterServices;
using System.Reflection;

namespace POS.Services.PrinterServices;

public class PrinterService : IPrinterServices
{
    private readonly IUnitOfWork _unitOfWork;

    public PrinterService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<OrderSetting?> CreateOrderSettingAsync(OrderSetting orderSetting)
    {
        if (orderSetting is null) return null;

        try
        {
            await _unitOfWork.Repository<OrderSetting>().AddAsync(orderSetting);
            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return null;
        }
        catch (Exception ex)
        {
            Log.Error($"Cant Create Printer {ex.Message}");
            return null;
        }
        return orderSetting;
    }

    public async Task<KitchenType?> CreatePrinterAsync(KitchenType printer)
    {
        if (printer is null) return null;

        try
        {
            await _unitOfWork.Repository<KitchenType>().AddAsync(printer);
            var result = await _unitOfWork.CompleteAsync();
            if (result <= 0)
                return null;
        }
        catch (Exception ex)
        {
            Log.Error($"Cant Create Printer {ex.Message}");
            return null;
        }
        return printer;
    }

    public async Task<KitchenType?> GetKitchenByIdAsync(int kitchenId)
    {
        var existingPrinter = await _unitOfWork.Repository<KitchenType>().GetByIdAsync(kitchenId);

        if (existingPrinter is null) return null;

        return existingPrinter;
    }

    public async Task<List<KitchenType>?> GetKitchenTypesAsync()
    {
        var kitchenTypes = await _unitOfWork.Repository<KitchenType>().GetAllAsync();

        if (kitchenTypes is null) return null;

        return kitchenTypes!.ToList() ?? [];
    }

    public async Task<OrderSetting?> GetOrderSettingByIdAsync(int orderSettingId)
    {
        try
        {
            return await _unitOfWork.Repository<OrderSetting>().GetByIdAsync(orderSettingId);
        }
        catch (Exception ex)
        {
            Log.Error($"Error fetching OrderSetting by ID {orderSettingId}: {ex.Message}");
            return null;
        }
    }

    public async Task<List<OrderSetting>?> GetOrderSettingsAsync()
    {
        try
        {
            var orderSettings = await _unitOfWork.Repository<OrderSetting>().GetAllAsync();
            if (orderSettings is null) return null;
            return orderSettings.ToList();
        }
        catch (Exception ex)
        {
            Log.Error($"Error fetching all OrderSettings: {ex.Message}");
            return null;
        }
    }


    public async Task<KitchenType?> UpdateKitchenTypesAsync(KitchenType printer)
    {
        if (printer is null) return null;

        try
        {
            var existingPrinter = await _unitOfWork.Repository<KitchenType>().GetByIdAsync(printer.Id);
            if (existingPrinter is null) return null;

            existingPrinter.KitchenName = printer.KitchenName;
            existingPrinter.BranchId = existingPrinter.BranchId;

            _unitOfWork.Repository<KitchenType>().Update(existingPrinter);
            var result = await _unitOfWork.CompleteAsync();

            if (result <= 0) return null;
        }
        catch (Exception ex)
        {
            Log.Error($"Error updating KitchenType: {ex.Message}");
            return null;
        }

        return printer;
    }

    public async Task<OrderSetting?> UpdateOrderSettingAsync(OrderSetting orderSetting)
    {
        if (orderSetting is null) return null;
        try
        {
            var existingOrderSetting = await _unitOfWork.Repository<OrderSetting>().GetByIdAsync(orderSetting.Id);
            if (existingOrderSetting is null) return null;

            existingOrderSetting.BranchID = existingOrderSetting.BranchID;
            existingOrderSetting.OrderType = existingOrderSetting.OrderType;
            existingOrderSetting.OrderStatment = orderSetting.OrderStatment;
            existingOrderSetting.Service = orderSetting.Service;
            existingOrderSetting.Tax = orderSetting.Tax;
            existingOrderSetting.Tips = orderSetting.Tips;
            existingOrderSetting.JobID = orderSetting.JobID;
            existingOrderSetting.CustomerReceiptCount = orderSetting.CustomerReceiptCount;
            existingOrderSetting.FullKitchenReceiptCount = orderSetting.FullKitchenReceiptCount;
            existingOrderSetting.SeparateReceiptCount = orderSetting.SeparateReceiptCount;
            existingOrderSetting.ClosingReceiptCount = orderSetting.ClosingReceiptCount;
            existingOrderSetting.AddServiceToItemPrice = orderSetting.AddServiceToItemPrice;

            _unitOfWork.Repository<OrderSetting>().Update(existingOrderSetting);
            var result = await _unitOfWork.CompleteAsync();

            if (result <= 0) return null;
        }
        catch (Exception ex)
        {
            Log.Error($"Error updating OrderSetting: {ex.Message}");
            return null;
        }

        return orderSetting;
    }


}
