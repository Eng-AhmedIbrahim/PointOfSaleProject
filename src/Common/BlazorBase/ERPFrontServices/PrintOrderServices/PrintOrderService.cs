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
    public Task PrintDineInOrder(DineInOrderDetails orderId)
    {
        throw new NotImplementedException();
    }

    public async Task<bool> PrintTakeAwayOrder(decimal paid = 0.00m, string customerName = "", string customerPhone = "")
    {
        BackupMainOrderDtoDetails(paid, customerName, customerPhone);
        var result = await _orderSettingsService.CreateOrderAsync(_commonProperties.OrderDto!);
        if (result is null)
            return false;

        return true;
    }   

    private void BackupMainOrderDtoDetails(decimal paid, string customerName, string customerPhone)
    {
        _commonProperties.OrderDto!.OrderId = _commonProperties.CurrentOrderId;
        _commonProperties.OrderDto!.OrderType = "TakeAway";
        _commonProperties.OrderDto.CashierName = _commonProperties.CurrentUser;
        _commonProperties.OrderDto.BranchId = _commonProperties.BranchDetails!.Id;
        _commonProperties.OrderDto.BranchName = _commonProperties.BranchDetails.Name;
        _commonProperties.OrderDto.CashierId = _commonProperties.CurrentUserId;
        _commonProperties.OrderDto.FooterMessage = _commonProperties.TakeAwaySettings!.OrderStatment;
        _commonProperties.OrderDto.PaymentMethod = PaymentMethod.Cash;
        _commonProperties.OrderDto.Paid = paid > 0.00m ? paid : _commonProperties.TotalOrderPrice;
        _commonProperties.OrderDto.Remaining = _commonProperties.TotalOrderPrice - _commonProperties.OrderDto.Paid;
        _commonProperties.OrderDto.SubTotal = _commonProperties.SubTotal;
        _commonProperties.OrderDto.Services = _commonProperties.TakeAwaySettings!.Service;
        _commonProperties.OrderDto.Tax = _commonProperties.TakeAwaySettings!.Tax;
        _commonProperties.OrderDto.TotalOrderDiscount = _commonProperties.TotalDiscount;
        _commonProperties.OrderDto.GrandTotal = _commonProperties.TotalOrderPrice;
        _commonProperties.OrderDto.OrderDate = _commonProperties.PosDate?.ToDateTime(TimeOnly.FromDateTime(DateTime.Now));
        _commonProperties.OrderDto.OrderState = "Completed";
        _commonProperties.OrderDto.OrderNotice = _commonProperties.OrderNote;
        _commonProperties.OrderDto.OrderDetails = _commonProperties.TableItems;
        _commonProperties.OrderDto.CustomerName = customerName;
        _commonProperties.OrderDto.CustomerPhone = customerPhone;
        _commonProperties.OrderDto.OrderSettings = _commonProperties.OrderSettings;
    }
}
