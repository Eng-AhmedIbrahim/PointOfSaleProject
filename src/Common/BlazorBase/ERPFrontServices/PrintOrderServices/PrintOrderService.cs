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

    public async Task PrintInitialDineInOrder(DineInOrderDetails orderId, bool printCustomer = true, bool printKitchen = true, bool isClosing = false)
    {
        BackupMainOrderDtoDetails(null!, null!, null!);
        BackupDineInDate(orderId);
        
        // Populate the OrderDto with DineIn specifics
        _commonProperties.OrderDto!.OrderType = "DineIn";
        _commonProperties.OrderDto.OrderDetails = orderId.BasicOrderDetails?.Items;
        _commonProperties.OrderDto.SubTotal = orderId.BasicOrderDetails?.Account;
        _commonProperties.OrderDto.GrandTotal = orderId.BasicOrderDetails?.Total;
        _commonProperties.OrderDto.OrderId = orderId.BasicOrderDetails?.OrderId ?? _commonProperties.CurrentOrderId;
        _commonProperties.OrderDto.FooterMessage = _commonProperties.DineInSettings?.OrderStatment;
        _commonProperties.OrderDto.Tax = orderId.BasicOrderDetails?.Tax;
        _commonProperties.OrderDto.Services = orderId.BasicOrderDetails?.Service;

        await _orderSettingsService.CreateOrderAsync(_commonProperties.OrderDto!);
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
        _commonProperties.OrderDto!.OrderType = _commonProperties.CurrentPosMode;
        _commonProperties.OrderDto.CashierName = _commonProperties.CurrentUser;
        _commonProperties.OrderDto.BranchId = _commonProperties.BranchDetails?.Id ?? 1;
        _commonProperties.OrderDto.BranchName = _commonProperties.BranchDetails?.Name;
        _commonProperties.OrderDto.CashierId = _commonProperties.CurrentUserId;
        
        var settings = _commonProperties.CurrentPosMode switch
        {
            "TakeAway" => (dynamic?)_commonProperties.TakeAwaySettings,
            "DineIn" => (dynamic?)_commonProperties.DineInSettings,
            "Delivery" => (dynamic?)_commonProperties.DeliverySettings,
            _ => null
        };

        _commonProperties.OrderDto.FooterMessage = settings?.OrderStatment;
        _commonProperties.OrderDto.PaymentMethod = paymentMethod;
        _commonProperties.OrderDto.Paid = paid > 0.00m ? paid : (_commonProperties._financeSettingsList?.Count > 4 ? _commonProperties._financeSettingsList[4].Value : 0M);
        _commonProperties.OrderDto.Remaining = (_commonProperties._financeSettingsList?.Count > 4 ? _commonProperties._financeSettingsList[4].Value : 0M) - _commonProperties.OrderDto.Paid;
        _commonProperties.OrderDto.SubTotal = _commonProperties._financeSettingsList?.Count > 0 ? _commonProperties._financeSettingsList[0].Value : 0M;
        _commonProperties.OrderDto.Services = settings?.Service;
        _commonProperties.OrderDto.Tax = settings?.Tax;
        _commonProperties.OrderDto.TotalOrderDiscount = _commonProperties.TotalDiscount;
        _commonProperties.OrderDto.GrandTotal = _commonProperties._financeSettingsList?.Count > 4 ? _commonProperties._financeSettingsList[4].Value : 0M;
        _commonProperties.OrderDto.OrderDate = _commonProperties.PosDate?.ToDateTime(TimeOnly.FromDateTime(DateTime.Now));
        _commonProperties.OrderDto.OrderState = "Completed";
        _commonProperties.OrderDto.OrderNotice = _commonProperties.OrderNote;
        _commonProperties.OrderDto.OrderDetails = _commonProperties.TableItems;
        _commonProperties.OrderDto.CustomerName = customerName;
        _commonProperties.OrderDto.CustomerPhone = customerPhone;
        _commonProperties.OrderDto.OrderSettings = _commonProperties.OrderSettings;
        
        // Populate Discount Details
        if (_commonProperties.OrderDiscount != null)
        {
            _commonProperties.OrderDto.DiscountReason = _commonProperties.OrderDiscount.DiscountReason;
            _commonProperties.OrderDto.DiscountType = _commonProperties.OrderDiscount.DiscountType;
            _commonProperties.OrderDto.DiscountPercentage = _commonProperties.OrderDiscount.Percentage;
        }

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

    public async Task<bool> ReprintOrderAsync(int orderId)
    {
        // Currently just returning true to satisfy interface.
        // Logic can be expanded if needed for non-desktop scenarios.
        return await Task.FromResult(true);
    }

    public Task PrintDineInClosingReceipt(DineInOrderDetails orderId)
    {
        return Task.CompletedTask;
    }
}