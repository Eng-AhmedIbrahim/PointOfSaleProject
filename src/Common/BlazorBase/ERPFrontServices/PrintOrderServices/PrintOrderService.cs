namespace BlazorBase.ERPFrontServices.PrintOrderServices;

public class PrintOrderService : IPrintOrderService
{
    private readonly CommonProperties _commonProperties;
    private readonly OrderSettingsService _orderSettingsService;

    public PrintOrderService(CommonProperties commonProperties,
        OrderSettingsService orderSettingsService)
    {
        _commonProperties = commonProperties;
        _orderSettingsService = orderSettingsService;
    }

    public Task PrintInitialDineInOrder(DineInOrderDetails orderId)
    {
        BackupMainOrderDtoDetails(null!, null!, null!);
        BackupDineInDate(orderId);

        return Task.CompletedTask;
    }

    private void BackupDineInDate(DineInOrderDetails orderId)
    {
        _commonProperties.OrderDto!.TakerID = _commonProperties.CurrentUserId;
        _commonProperties.OrderDto.TakerName = _commonProperties.CurrentUser;
        _commonProperties.OrderDto.TableId = orderId.RelatedTableId;
        _commonProperties.OrderDto.TableName = orderId.RelatedTableName;
        _commonProperties.OrderDto.WaiterId = orderId.CaptainId;
        _commonProperties.OrderDto.WaiterName = orderId.CaptainName;
    }

    public async Task<bool> PrintTakeAwayOrder(decimal paid = 0, string customerName = "", string customerPhone = "", PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        BackupMainOrderDtoDetails(customerName, customerPhone, paid, paymentMethod);
        var result = await _orderSettingsService.CreateOrderAsync(_commonProperties.OrderDto!);
        if (result is null)
            return false;

        return true;
    }

    private void BackupMainOrderDtoDetails(string customerName, string customerPhone, decimal? paid = 0.00m, PaymentMethod paymentMethod = PaymentMethod.Cash)
    {
        _commonProperties.OrderDto!.OrderId = _commonProperties.CurrentOrderId;
        _commonProperties.OrderDto!.OrderType = "TakeAway";
        _commonProperties.OrderDto.CashierName = _commonProperties.CurrentUser;
        _commonProperties.OrderDto.BranchId = _commonProperties.BranchDetails!.Id;
        _commonProperties.OrderDto.BranchName = _commonProperties.BranchDetails.Name;
        _commonProperties.OrderDto.CashierId = _commonProperties.CurrentUserId;
        _commonProperties.OrderDto.FooterMessage = _commonProperties.TakeAwaySettings!.OrderStatment;
        _commonProperties.OrderDto.PaymentMethod = paymentMethod;
        _commonProperties.OrderDto.Paid = paid > 0.00m ? paid : _commonProperties._financeSettingsList![4].Value;
        _commonProperties.OrderDto.Remaining = _commonProperties._financeSettingsList![4].Value - _commonProperties.OrderDto.Paid;
        _commonProperties.OrderDto.SubTotal = _commonProperties._financeSettingsList![0].Value;
        _commonProperties.OrderDto.Services = _commonProperties.TakeAwaySettings!.Service;
        _commonProperties.OrderDto.Tax = _commonProperties.TakeAwaySettings!.Tax;
        _commonProperties.OrderDto.TotalOrderDiscount = _commonProperties.TotalDiscount;
        _commonProperties.OrderDto.GrandTotal = _commonProperties._financeSettingsList![4].Value;
        _commonProperties.OrderDto.OrderDate = _commonProperties.PosDate?.ToDateTime(TimeOnly.FromDateTime(DateTime.Now));
        _commonProperties.OrderDto.OrderState = "Completed";
        _commonProperties.OrderDto.OrderNotice = _commonProperties.OrderNote;
        _commonProperties.OrderDto.OrderDetails = _commonProperties.TableItems;
        _commonProperties.OrderDto.CustomerName = customerName;
        _commonProperties.OrderDto.CustomerPhone = customerPhone;
        _commonProperties.OrderDto.OrderSettings = _commonProperties.OrderSettings;
        _commonProperties.OrderDto.TotalDiscount = GetTotalDiscountAmount();
        _commonProperties.OrderDto.DiscountBy = _commonProperties._financeSettingsList![1].Value != 0 ? _commonProperties.CurrentUserId : null;
        _commonProperties.OrderDto.DiscountByName = _commonProperties._financeSettingsList![1].Value != 0 ? _commonProperties.CurrentUser : null;
    }
    private decimal? GetTotalDiscountAmount()
    {
        if (string.IsNullOrEmpty(_commonProperties.OrderDto!.DiscountType))
            return null;

        if (_commonProperties.OrderDto!.DiscountType == "percentage")
            return _commonProperties._financeSettingsList![4].Value - (_commonProperties._financeSettingsList![4].Value * (_commonProperties._financeSettingsList![1].Value / 100));

        if (_commonProperties.OrderDto.DiscountType == "value")
            return _commonProperties._financeSettingsList![4].Value;

        return null;
    }

}